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
    IReadOnlyList<ulong>? StartupMonikerBundleIds = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    // Set only by LoadFile. Paths in a manifest are relative to that manifest and
    // never resolved against the process working directory.
    private string? ConfigurationRoot { get; init; }

    public static ExperienceManifest LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var content = File.ReadAllBytes(fullPath);
        return Parse(content) with { ConfigurationRoot = Path.GetDirectoryName(fullPath) };
    }

    public bool UsesRepositoryArtifacts =>
        Bundles.Count > 0 && Bundles.All(bundle => bundle.HasArtifactIdentity);

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

        var startupBundles = Bundles
            .Where(bundle => startupIds.Contains(bundle.BundleId))
            .ToArray();

        if (startupBundles.Length == 0)
            return fallbackMoniker?.CreateStartupMonikerManifest();

        return this with
        {
            Bundles = startupBundles,
            StartupMonikerBundleIds = monikerIds,
            Intent = null
        };
    }

    public RuntimeManifest ToRuntimeManifest()
    {
        var requests = Bundles
            .Select(bundle => new MicroBundleManifestEntry(
                bundle.BundleId,
                ResolveBundleVersion(bundle)))
            .ToArray();

        return new RuntimeManifest(RuntimeId, requests, Intent: Intent);
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
                        throw new InvalidOperationException(
                            $"MicroBundle {bundle.BundleId} uses configurationPath, but the manifest was not loaded from a file.");

                    var path = ResolveConfigurationPath(configurationRoot, bundle.ConfigurationPath!);
                    var info = new FileInfo(path);
                    if (!info.Exists)
                        throw new FileNotFoundException(
                            $"Configuration file for MicroBundle {bundle.BundleId} was not found.", path);
                    if (info.Length > MaximumConfigurationFileBytes)
                        throw new InvalidOperationException(
                            $"Configuration file for MicroBundle {bundle.BundleId} exceeds {MaximumConfigurationFileBytes} bytes.");
                    bytes = File.ReadAllBytes(path);
                }
                else
                {
                    bytes = Convert.FromBase64String(bundle.ConfigurationBase64);
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
            var rootPrefix = fullRoot.EndsWith(Path.DirectorySeparatorChar)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
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

public sealed record ExperienceBundleRequest(
    ulong BundleId,
    string ConfigurationBase64,
    string? ArtifactVersion = null,
    string? ContentHash = null,
    string? BundleVersion = null,
    string? ConfigurationPath = null)
{
    public bool HasArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ContentHash);
}
