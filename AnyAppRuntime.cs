using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppRuntime
{
    public const ulong RuntimeId = 2111;

    public static RuntimeAssembly Compose(RuntimeManifest? manifest = null)
    {
        manifest ??= MonikerExperience.CreateManifest();

        var catalog = manifest.Bundles.Count == 0
            ? new SingleBundleCatalog()
            : new SingleBundleCatalog(new MonikerMicroBundle());
        var cos = new FsmCos(catalog);

        return cos.Execute(manifest);
    }

    private sealed class SingleBundleCatalog : IMicroBundleCatalog
    {
        private readonly IMicroBundle? _bundle;

        public SingleBundleCatalog(IMicroBundle? bundle = null)
        {
            _bundle = bundle;
        }

        public bool TryResolve(ulong bundleId, out IMicroBundle? bundle)
        {
            if (_bundle is not null && _bundle.Id == bundleId)
            {
                bundle = _bundle;
                return true;
            }

            bundle = null;
            return false;
        }
    }
}