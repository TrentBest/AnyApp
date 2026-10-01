using System.Globalization;
using TheSingularityWorkshop.FSM_API;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The first configured AnyApp Experience. The manifest composes the static
/// Moniker MicroBundle; presentation behavior is applied only when this
/// Experience executes, keeping composition preview motion-free.
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

    /// <summary>
    /// Executes the Moniker presentation layer over an already composed surface.
    /// The MicroBundle itself remains static and previewable.
    /// </summary>
    public static GuiNode ExecutePresentation(GuiNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var phaseIndex = 0;
        return ApplyWave(root, ref phaseIndex);
    }

    private static GuiNode ApplyWave(GuiNode node, ref int phaseIndex)
    {
        var properties = new Dictionary<string, string>(node.Properties, StringComparer.Ordinal);

        if (node.Kind == GuiKinds.Text &&
            node.Id.StartsWith("moniker-", StringComparison.Ordinal))
        {
            properties["wavePeriod"] = "3.8";
            properties["waveAmplitude"] = "7";
            properties["waveRotation"] = "2.5";
            properties["wavePhase"] =
                ((phaseIndex++ % 12) / 12d).ToString(CultureInfo.InvariantCulture);
        }

        var children = new GuiNode[node.Children.Count];
        for (var index = 0; index < node.Children.Count; index++)
            children[index] = ApplyWave(node.Children[index], ref phaseIndex);

        return new GuiNode(
            node.Kind,
            node.Id,
            node.Text,
            node.Source,
            properties,
            children);
    }

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
