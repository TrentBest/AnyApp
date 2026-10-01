using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppRuntime
{
    public const ulong RuntimeId = MonikerExperience.RuntimeId;

    public static RuntimeAssembly Compose(RuntimeManifest? manifest = null)
    {
        manifest ??= MonikerExperience.CreateManifest();

        var cos = new FsmCos(MonikerExperience.CreateCatalog());
        return cos.Execute(manifest);
    }
}
