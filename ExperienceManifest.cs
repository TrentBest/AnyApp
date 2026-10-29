using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.FSM_UserIO;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Serialized, host-neutral Experience publication manifest.
/// </summary>
public sealed record ExperienceManifest(
    ulong ExperienceId,
    string Version,
    ulong RuntimeId,
    IReadOnlyList<ExperienceBundleRequest> Bundles,
    SemanticIntent? Intent = null,
    IReadOnlyList<ulong>? StartupMonikerBundleIds = null,
    ExperienceResolvedPlan? ResolvedPlan = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    // Set only by LoadFile. Paths in a manifest are relative to that manifest and
    // never resolved against the process working directory.
    internal string? ConfigurationRoot { get; init; }

    /// <summary>
    /// Immutable identities for the full published artifact closure. This catalogue does not
    /// create FSM_COS composition roots; those remain the entries in <see cref="Bundles"/>.
    /// Null preserves compatibility with older flat manifests whose roots also carried identities.
    /// </summary>
    public IReadOnlyList<ExperienceArtifactIdentity>? ArtifactCatalogue { get; init; }

    public IReadOnlyList<ExperienceArtifactIdentity> GetArtifactCatalogue() =>
        ArtifactCatalogue is not null
            ? ArtifactCatalogue
            : Bundles.Where(bundle => bundle.HasArtifactIdentity)
                .Select(bundle => new ExperienceArtifactIdentity(
                    bundle.BundleId, bundle.ArtifactVersion!, bundle.ContentHash!))
                .ToArray();

    public static ExperienceManifest LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var content = File.ReadAllBytes(fullPath);
        return Parse(content) with { ConfigurationRoot = Path.GetDirectoryName(fullPath) };
    }

    public bool UsesRepositoryArtifacts =>
        Bundles.Count > 0 && Bundles.All(bundle => bundle.HasArtifactIdentity);

    public bool UsesResolvedRepositoryConfigurations =>
        Bundles.Count > 0 && Bundles.All(bundle => bundle.HasConfigurationArtifactIdentity);

    /// <summary>
    /// The ordered Moniker MicroBundles to present before the destination Experience.
    /// Legacy manifests infer the Workshop Moniker only when they already include it.
    /// </summary>
    public IReadOnlyList<ulong> GetStartupMonikerBundleIds()
    {
        if (StartupMonikerBundleIds is { Count: > 0 })
            return StartupMonikerBundleIds.Distinct().ToArray();

        return Bundles.Any(bundle => bundle.BundleId == MonikerExperience.MonikerBundleId)
            ? [MonikerExperience.MonikerBundleId]
            : [];
    }

    /// <summary>
    /// Creates the startup-only composition used to put the Moniker on screen while
    /// the full destination manifest is composing independently in the background.
    /// </summary>
    public ExperienceManifest? CreateStartupMonikerManifest(
        ExperienceManifest? fallbackMoniker = null)
    {
        var monikerIds = GetStartupMonikerBundleIds();
        if (monikerIds.Count == 0)
            return fallbackMoniker?.CreateStartupMonikerManifest();

        var startupIds = monikerIds.ToHashSet();

        // The Workshop Moniker's motion and renderer are separate MicroBundles.
        // Include them in the startup composition without making them the destination.
        if (startupIds.Contains(MonikerExperience.MonikerBundleId))
        {
            startupIds.Add(MonikerExperience.MotionBundleId);
            startupIds.Add(MonikerExperience.RendererBundleId);
        }

        if (ResolvedPlan is not null)
        {
            var artifactsById = GetArtifactCatalogue().ToDictionary(artifact => artifact.BundleId);
            var closure = new HashSet<ulong>();
            var pending = new Stack<ulong>(startupIds);
            while (pending.Count > 0)
            {
                var bundleId = pending.Pop();
                if (!closure.Add(bundleId))
                    continue;
                if (!artifactsById.ContainsKey(bundleId))
                    throw new InvalidOperationException(
                        $"Resolved startup Moniker references MicroBundle {bundleId}, which is absent from the published artifact catalogue.");

                foreach (var dependency in ResolvedPlan.Dependencies.Where(edge => edge.BundleId == bundleId))
                    pending.Push(dependency.DependencyId);
            }

            var loadOrder = ResolvedPlan.LoadOrder.Where(closure.Contains).ToArray();
            if (loadOrder.Length != closure.Count)
                throw new InvalidOperationException(
                    "The published startup Moniker closure does not match its recorded dependency-first load order.");

            var startupPlan = ResolvedPlan with
            {
                LoadOrder = loadOrder,
                Dependencies = ResolvedPlan.Dependencies
                    .Where(edge => closure.Contains(edge.BundleId) && closure.Contains(edge.DependencyId))
                    .ToArray()
            };

            return this with
            {
                Bundles = Bundles.Where(bundle => startupIds.Contains(bundle.BundleId)).ToArray(),
                ArtifactCatalogue = loadOrder.Select(bundleId => artifactsById[bundleId]).ToArray(),
                StartupMonikerBundleIds = monikerIds,
                Intent = null,
                ResolvedPlan = startupPlan
            };
        }

        var startupBundles = Bundles
            .Where(bundle => startupIds.Contains(bundle.BundleId))
            .ToArray();

        if (startupBundles.Length == 0)
            return fallbackMoniker?.CreateStartupMonikerManifest();

        return this with
        {
            Bundles = startupBundles,
            StartupMonikerBundleIds = monikerIds,
            Intent = null,
            ResolvedPlan = null
        };
    }

    public RuntimeManifest ToRuntimeManifest()
    {
        var requests = Bundles
            .Select(bundle => new MicroBundleManifestEntry(
                bundle.BundleId,
                ResolveBundleVersion(bundle)))
            .ToArray();

        if (ResolvedPlan is null)
            return new RuntimeManifest(RuntimeId, requests, Intent: Intent);

#if WORKSHOP_USE_REPOSITORY_SOURCES
        if (!UsesRepositoryArtifacts)
            throw new InvalidOperationException(
                "A pre-resolved Experience manifest must identify immutable artifacts for every MicroBundle.");
        if (!UsesResolvedRepositoryConfigurations)
            throw new InvalidOperationException(
                "A pre-resolved Experience manifest must identify immutable resolved-configuration artifacts for every MicroBundle.");

        var artifactsById = GetArtifactCatalogue().ToDictionary(artifact => artifact.BundleId);
        var loadPlan = ResolvedPlan.LoadOrder.Select(bundleId =>
        {
            if (!artifactsById.TryGetValue(bundleId, out var artifact))
                throw new InvalidOperationException(
                    $"Resolved load order references MicroBundle {bundleId}, which is absent from the Experience artifact catalogue.");
            if (!artifact.HasArtifactIdentity)
                throw new InvalidOperationException(
                    $"Resolved MicroBundle {bundleId} has no immutable artifact version/content hash.");

            var bundleVersion = Bundles.FirstOrDefault(bundle => bundle.BundleId == bundleId);
            var version = bundleVersion is null
                ? artifact.ArtifactVersion
                : ResolveBundleVersion(bundleVersion);
            return new RuntimeManifestEntry(
                new MicroBundleReference(bundleId, version, artifact.ContentHash),
                ManifestLoadStage.Resident);
        }).ToArray();
        var dependencies = ResolvedPlan.Dependencies
            .Select(edge => new RuntimeManifestDependency(edge.BundleId, edge.DependencyId))
            .ToArray();
        var schedule = new RuntimeManifestSchedule(loadPlan, dependencies);

        return new RuntimeManifest(
            RuntimeId,
            requests,
            LoadPlan: loadPlan,
            Schedule: schedule,
            Intent: Intent,
            ArbitrationResolved: true,
            PublishedArbitrationRounds: ResolvedPlan.ArbitrationRounds);
#else
        throw new InvalidOperationException(
            "This FSM_COS package does not expose the publish-resolved runtime contract; use the matching repository-source development graph.");
#endif
    }

    /// <summary>
    /// Creates the version-scoped configuration source consumed by FSM_COS alpha6.
    /// Configuration remains separate from the versioned bundle manifest.
    /// </summary>
    public IMicroBundleConfigurationSource CreateConfigurationSource() =>
        new ExperienceConfigurationSource(RuntimeId, Bundles, ConfigurationRoot);

    public static ExperienceManifest Parse(ReadOnlyMemory<byte> content) =>
        JsonSerializer.Deserialize<ExperienceManifest>(content.Span, JsonOptions)
        ?? throw new InvalidOperationException("The Experience artifact did not contain a manifest.");

    private static string ResolveBundleVersion(ExperienceBundleRequest bundle)
    {
        if (!string.IsNullOrWhiteSpace(bundle.BundleVersion))
            return bundle.BundleVersion;

        if (!string.IsNullOrWhiteSpace(bundle.ArtifactVersion))
            return bundle.ArtifactVersion;

        // Legacy local manifests predate explicit per-bundle versions. Their current
        // MicroBundles are version 1.0.0; new manifests should state BundleVersion.
        return "1.0.0";
    }

    private sealed class ExperienceConfigurationSource : IMicroBundleConfigurationSource
    {
        private const long MaximumConfigurationFileBytes = 64L * 1024 * 1024;
        private readonly ulong _runtimeId;
        private readonly IReadOnlyDictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>> _values;

        public ExperienceConfigurationSource(
            ulong runtimeId,
            IReadOnlyList<ExperienceBundleRequest> bundles,
            string? configurationRoot)
        {
            _runtimeId = runtimeId;
            var values = new Dictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>>();

            foreach (var bundle in bundles)
            {
                var hasInlineConfiguration = !string.IsNullOrWhiteSpace(bundle.ConfigurationBase64);
                var hasFileConfiguration = !string.IsNullOrWhiteSpace(bundle.ConfigurationPath);
                if (hasInlineConfiguration && hasFileConfiguration)
                    throw new InvalidOperationException(
                        $"MicroBundle {bundle.BundleId} declares both inline and file-based configuration.");

                if (!hasInlineConfiguration && !hasFileConfiguration)
                    continue;

                byte[] bytes;
                if (hasFileConfiguration)
                {
                    if (string.IsNullOrWhiteSpace(configurationRoot))
                    {
                        if (bundle.HasConfigurationArtifactIdentity)
                            continue; // The repository-backed source supplies this immutable configuration.
                        throw new InvalidOperationException(
                            $"MicroBundle {bundle.BundleId} uses configurationPath, but the manifest was not loaded from a file.");
                    }

                    var path = ResolveConfigurationPath(configurationRoot, bundle.ConfigurationPath!);
                    var info = new FileInfo(path);
                    if (!info.Exists)
                    {
                        if (bundle.HasConfigurationArtifactIdentity)
                            continue; // Permit repository/cache fallback for a missing sidecar.
                        throw new FileNotFoundException(
                            $"Configuration file for MicroBundle {bundle.BundleId} was not found.", path);
                    }
                    if (info.Length > MaximumConfigurationFileBytes)
                        throw new InvalidOperationException(
                            $"Configuration file for MicroBundle {bundle.BundleId} exceeds {MaximumConfigurationFileBytes} bytes.");
                    bytes = File.ReadAllBytes(path);
                }
                else
                {
                    bytes = Convert.FromBase64String(bundle.ConfigurationBase64);
                }

                if (!string.IsNullOrWhiteSpace(bundle.ConfigurationContentHash))
                {
                    var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                    if (!string.Equals(actualHash, bundle.ConfigurationContentHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Configuration for MicroBundle {bundle.BundleId} does not match its published SHA-256 identity.");
                }

                var key = (bundle.BundleId, ResolveBundleVersion(bundle));
                if (!values.TryAdd(key, bytes))
                    throw new InvalidOperationException(
                        $"Experience manifest declares configuration more than once for MicroBundle {bundle.BundleId}.");
            }

            _values = values;
        }

        private static string ResolveConfigurationPath(string root, string relativePath)
        {
            if (Path.IsPathRooted(relativePath))
                throw new InvalidOperationException("MicroBundle configurationPath must be relative to its manifest.");

            var fullRoot = Path.GetFullPath(root);
            var fullPath = Path.GetFullPath(Path.Combine(fullRoot, relativePath));
            var relativeToRoot = Path.GetRelativePath(fullRoot, fullPath);
            if (Path.IsPathRooted(relativeToRoot) ||
                relativeToRoot.Equals("..", StringComparison.Ordinal) ||
                relativeToRoot.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                relativeToRoot.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "MicroBundle configurationPath must remain inside the manifest directory.");

            return fullPath;
        }

        public bool TryGetConfiguration(
            ulong runtimeId,
            ulong bundleId,
            string version,
            out ReadOnlyMemory<byte> configuration)
        {
            if (runtimeId == _runtimeId &&
                _values.TryGetValue((bundleId, version), out configuration))
                return true;

            configuration = default;
            return false;
        }
    }
}


/// <summary>
/// The immutable, publish-resolved composition plan carried by an Experience manifest.
/// LoadOrder is the dependency-first flat closure; Dependencies records each edge as
/// (bundle -> prerequisite). ArbitrationRounds is the zero-based convergence round.
/// </summary>
public sealed record ExperienceResolvedPlan(
    int ArbitrationRounds,
    IReadOnlyList<ulong> LoadOrder,
    IReadOnlyList<ExperienceResolvedDependency> Dependencies);

/// <summary>A directed MicroBundle dependency edge in a resolved Experience plan.</summary>
public sealed record ExperienceResolvedDependency(ulong BundleId, ulong DependencyId);

/// <summary>An immutable address identity for one artifact in a published dependency closure.</summary>
public sealed record ExperienceArtifactIdentity(
    ulong BundleId,
    string ArtifactVersion,
    string ContentHash)
{
    public bool HasArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ContentHash);
}

public sealed record ExperienceBundleRequest(
    ulong BundleId,
    string ConfigurationBase64 = "",
    string? ArtifactVersion = null,
    string? ContentHash = null,
    string? BundleVersion = null,
    string? ConfigurationPath = null,
    string? ConfigurationArtifactVersion = null,
    string? ConfigurationContentHash = null)
{
    public bool HasArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ContentHash);

    public bool HasConfigurationArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ConfigurationArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ConfigurationContentHash);
}
