using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppRuntime
{
    public const ulong RuntimeId = 2111;

    public static RuntimeAssembly Compose()
    {
        var bundle = new AnyAppMicroBundle();
        var catalog = new SingleBundleCatalog(bundle);
        var cos = new FsmCos(catalog);

        return cos.Execute(
            new RuntimeManifest(
                RuntimeId,
                [BundleRequest.Unconfigured(AnyAppMicroBundle.BundleId)]));
    }

    private sealed class SingleBundleCatalog : IMicroBundleCatalog
    {
        private readonly IMicroBundle _bundle;

        public SingleBundleCatalog(IMicroBundle bundle)
        {
            _bundle = bundle;
        }

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
}