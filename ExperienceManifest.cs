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

    /// <summary>
    /// Converts the publication manifest into the machine-oriented FSM_COS manifest.
    /// The Workshop Moniker is a host bootstrap surface and is therefore always
    /// present before the selected Experience bundles.
    /// </summary>
    public RuntimeManifest ToRuntimeManifest()
    {
        var requests = Bundles
            .Select(bundle => new BundleRequest(
                bundle.BundleId,
                string.IsNullOrWhiteSpace(bundle.ConfigurationBase64)
                    ? ReadOnlyMemory<byte>.Empty
                    : Convert.FromBase64String(bundle.ConfigurationBase64)))
            .ToArray();

        if (!requests.Any(request => request.BundleId == MonikerMicroBundle.BundleId))
        {
            requests =
            [
                new BundleRequest(MonikerMicroBundle.BundleId, ReadOnlyMemory<byte>.Empty),
                .. requests
            ];
        }

        return new RuntimeManifest(RuntimeId, requests);
    }

    public static ExperienceManifest Parse(ReadOnlyMemory<byte> content) =>
        JsonSerializer.Deserialize<ExperienceManifest>(content.Span, JsonOptions)
        ?? throw new InvalidOperationException("The Experience artifact did not contain a manifest.");
}

public sealed record ExperienceBundleRequest(
    ulong BundleId,
    string ConfigurationBase64);
