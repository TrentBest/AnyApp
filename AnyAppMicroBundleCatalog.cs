using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.Experiences.Moniker;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.Renderer;

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
            [MonikerWaveMotionMicroBundle.BundleId] = new MonikerWaveMotionMicroBundle(),
            [ForgeMicroBundle.BundleId] = new ForgeMicroBundle(),
            [RendererMicroBundle.BundleId] = new RendererMicroBundle(),
#if WORKSHOP_STORYBUILDER
            [StoryBuilderAuthoringBundle.BundleId] = new StoryBuilderAuthoringBundle(),
#endif
        };

    public bool TryResolve(ulong bundleId, out IMicroBundle? bundle) =>
        _bundles.TryGetValue(bundleId, out bundle);

    public bool TryResolve(ulong bundleId, string version, out IMicroBundle? bundle)
    {
        if (!_bundles.TryGetValue(bundleId, out bundle) || bundle is null)
            return false;

        if (string.IsNullOrWhiteSpace(version) ||
            string.Equals(bundle.Descriptor.Version, version, StringComparison.Ordinal))
            return true;

        bundle = null;
        return false;
    }
}
