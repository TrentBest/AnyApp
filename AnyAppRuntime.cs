using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppRuntime
{
    public const ulong RuntimeId = MonikerExperience.RuntimeId;

    public static RuntimeAssembly Compose(RuntimeManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var cos = new FsmCos(new AnyAppMicroBundleCatalog());
        return cos.Execute(manifest);
    }
}
