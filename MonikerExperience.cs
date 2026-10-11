using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.FSM_API;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The first configured AnyApp Experience. It composes static Moniker content
/// with a separately addressable motion MicroBundle.
/// </summary>
public static class MonikerExperience
{
    public const ulong ExperienceId = 3101;
    public const ulong RuntimeId = 3111;

    public static RuntimeManifest CreateManifest() =>
        new(
            RuntimeId,
            [
                MicroBundleDependencyRequest.Unconfigured(MonikerMicroBundle.BundleId),
                MicroBundleDependencyRequest.Unconfigured(MonikerWaveMotionMicroBundle.BundleId)
            ],
            new Context());

    public static IMicroBundleCatalog CreateCatalog() => new AnyAppMicroBundleCatalog();

    /// <summary>
    /// Applies the selected motion capability to a composed Moniker surface.
    /// The fallback preserves older manifests until they declare the motion bundle.
    /// </summary>
    public static GuiNode ExecutePresentation(GuiNode root, IMonikerMotion? motion = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        return (motion ?? new MonikerWaveMotionMicroBundle()).Apply(root);
    }

    private sealed class Context : IStateContext
    {
        public string Name { get; set; } = "Workshop.Moniker";
        public bool IsValid { get; set; } = true;
    }
}
