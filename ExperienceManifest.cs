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
    IReadOnlyList<ExperienceBundleRequest>? StartupBundles = null)
{
    public const ulong WorkshopMonikerBundleId = 3101UL;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public bool HasWorkshopMonikerStartup =>
        StartupBundles?.Any(bundle => bundle.BundleId == WorkshopMonikerBundleId) == true;

    public RuntimeManifest ToRuntimeManifest()
    {
        if (!HasWorkshopMonikerStartup)
        {
            throw new InvalidOperationException(
                "Every AnyApp Experience manifest must declare the Workshop Moniker as a startup MicroBundle.");
        }

        var requests = (StartupBundles ?? [])
            .Concat(Bundles)
            .GroupBy(bundle => bundle.BundleId)
            .Select(group => group.First())
            .Select(bundle => new BundleRequest(
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
    string ConfigurationBase64);
