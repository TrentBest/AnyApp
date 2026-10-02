using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The current native host catalog. Repository materialization will replace this
/// compiled catalog once published MicroBundles can be delivered independently.
/// </summary>
public sealed class AnyAppMicroBundleCatalog : IMicroBundleCatalog
{
    private readonly IReadOnlyDictionary<ulong, IMicroBundle> _bundles =
        new Dictionary<ulong, IMicroBundle>
        {
            [MonikerMicroBundle.BundleId] = new MonikerMicroBundle(),
            [ForgeMicroBundle.BundleId] = new ForgeMicroBundle()
        };

    public bool TryResolve(ulong bundleId, out IMicroBundle? bundle) =>
        _bundles.TryGetValue(bundleId, out bundle);
}
