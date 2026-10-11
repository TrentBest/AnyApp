using TheSingularityWorkshop.Experiences.Moniker;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.Renderer;
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
                new MicroBundleManifestEntry(MonikerMicroBundle.BundleId, "1.0.0"),
                new MicroBundleManifestEntry(MonikerWaveMotionMicroBundle.BundleId, "1.0.0"),
                new MicroBundleManifestEntry(RendererMicroBundle.BundleId, "0.1.0-alpha.2")
            ],
            new Context());

    public static IMicroBundleCatalog CreateCatalog() => new AnyAppMicroBundleCatalog();

    /// <summary>
    /// Applies the selected motion capability to a composed Moniker surface.
    /// The fallback preserves older manifests until they declare the motion bundle.
    /// </summary>
    public static GuiNode ExecutePresentation(
        GuiNode root,
        IMonikerMotion? motion = null,
        bool allowMotion = true)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!allowMotion)
            return root;

        return (motion ?? new MonikerWaveMotionMicroBundle()).Apply(root);
    }

    private sealed class Context : IStateContext
    {
        public string Name { get; set; } = "Workshop.Moniker";
        public bool IsValid { get; set; } = true;
    }
}
