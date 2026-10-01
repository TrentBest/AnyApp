using TheSingularityWorkshop.FSM_API;
using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The first configured AnyApp Experience. Its manifest contains the Moniker
/// MicroBundle and its context; the host does not hard-code the presentation.
/// </summary>
public static class MonikerExperience
{
    public const ulong ExperienceId = 3101;
    public const ulong RuntimeId = 3111;

    public static RuntimeManifest CreateManifest() =>
        new(
            RuntimeId,
            [BundleRequest.Unconfigured(MonikerMicroBundle.BundleId)],
            new Context());

    public static IMicroBundleCatalog CreateCatalog() =>
        new SingleBundleCatalog(new MonikerMicroBundle());

    private sealed class SingleBundleCatalog : IMicroBundleCatalog
    {
        private readonly IMicroBundle _bundle;

        public SingleBundleCatalog(IMicroBundle bundle) => _bundle = bundle;

        public bool TryResolve(ulong bundleId, out IMicroBundle? bundle)
        {
            if (_bundle.Id == bundleId)
            {
                bundle = _bundle;
                return true;
            }

            bundle = null;
            return false;
        }
    }

    private sealed class Context : IStateContext
    {
        public string Name { get; set; } = "Workshop.Moniker";
        public bool IsValid { get; set; } = true;
    }
}
