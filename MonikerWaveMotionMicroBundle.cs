using System.Globalization;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// A reusable motion capability for Moniker glyph surfaces.
/// The motion is an independent MicroBundle; the Moniker content remains static.
/// </summary>
public sealed class MonikerWaveMotionMicroBundle : IMicroBundle, IMonikerMotion
{
    public const ulong BundleId = 3102;
    public const string ProviderId = "workshop-moniker-motion-wave";

    public MicroBundleDescriptor Descriptor { get; } =
        new(BundleId, "1.0.0", providers: [new MicroBundleProvider(ProviderId)]);

    public ulong Id => Descriptor.Id;

    public IReadOnlyList<MicroBundleDependencyRequest> Dependencies => [];

    public void Load(IMicroBundleLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }

    public GuiNode Apply(GuiNode root)
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

    public bool Arbitrate(IMicroBundleArbitrationContext context, int roundIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return false;
    }
}

/// <summary>
/// Behavior contract implemented by independently composable Moniker motion bundles.
/// </summary>
public interface IMonikerMotion
{
    GuiNode Apply(GuiNode root);
}
