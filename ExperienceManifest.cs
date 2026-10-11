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
    SemanticIntent? Intent = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public bool UsesRepositoryArtifacts =>
        Bundles.Count > 0 && Bundles.All(bundle => bundle.HasArtifactIdentity);

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
        new ExperienceConfigurationSource(RuntimeId, Bundles);

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
        private readonly ulong _runtimeId;
        private readonly IReadOnlyDictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>> _values;

        public ExperienceConfigurationSource(
            ulong runtimeId,
            IReadOnlyList<ExperienceBundleRequest> bundles)
        {
            _runtimeId = runtimeId;
            var values = new Dictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>>();

            foreach (var bundle in bundles)
            {
                if (string.IsNullOrWhiteSpace(bundle.ConfigurationBase64))
                    continue;

                var key = (bundle.BundleId, ResolveBundleVersion(bundle));
                if (!values.TryAdd(key, Convert.FromBase64String(bundle.ConfigurationBase64)))
                    throw new InvalidOperationException(
                        $"Experience manifest declares configuration more than once for MicroBundle {bundle.BundleId}.");
            }

            _values = values;
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
    string? BundleVersion = null)
{
    public bool HasArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ContentHash);
}
