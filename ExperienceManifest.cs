using System.Text.Json;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Serialized, host-neutral Experience publication manifest.
/// </summary>
public sealed record ExperienceManifest(
    ulong ExperienceId,
    string Version,
    ulong RuntimeId,
    IReadOnlyList<ExperienceBundleRequest> Bundles)
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
            .Select(bundle => new MicroBundleDependencyRequest(
                bundle.BundleId,
                string.IsNullOrWhiteSpace(bundle.ConfigurationBase64)
                    ? ReadOnlyMemory<byte>.Empty
                    : Convert.FromBase64String(bundle.ConfigurationBase64)))
            .ToArray();

        return new RuntimeManifest(RuntimeId, requests);
    }

    public static ExperienceManifest Parse(ReadOnlyMemory<byte> content) =>
        JsonSerializer.Deserialize<ExperienceManifest>(content.Span, JsonOptions)
        ?? throw new InvalidOperationException("The Experience artifact did not contain a manifest.");
}

public sealed record ExperienceBundleRequest(
    ulong BundleId,
    string ConfigurationBase64,
    string? ArtifactVersion = null,
    string? ContentHash = null)
{
    public bool HasArtifactIdentity =>
        !string.IsNullOrWhiteSpace(ArtifactVersion) &&
        !string.IsNullOrWhiteSpace(ContentHash);
}
