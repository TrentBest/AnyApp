using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The Forge entry Experience. It is the user's diegetic Workshop entry point:
/// composition produces the space, while the host wires navigation into it.
/// </summary>
public sealed class ForgeMicroBundle : IMicroBundle, IAnyAppSurface
{
    public const ulong ExperienceId = 3201;
    public const ulong RuntimeId = 3211;
    public const ulong BundleId = 3201;
    public const string ProviderId = "workshop-forge";

    private GuiNode? _root;

    public MicroBundleDescriptor Descriptor { get; } =
        new(BundleId, "1.0.0", providers: [new MicroBundleProvider(ProviderId)]);

    public ulong Id => Descriptor.Id;

    public IReadOnlyList<BundleRequest> Dependencies => [];

    public GuiNode Root =>
        _root ?? throw new InvalidOperationException(
            "The Forge GUI surface is not available until FSM_COS has loaded the bundle.");

    public void Load(MicroBundleLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var portal = GuiBuilders.Button(
                "forge-explore",
                "EXPLORE EXPERIENCES  →")
            .Property("width", "360")
            .Property("height", "72")
            .Property("margin", "12")
            .Property("fontSize", "18")
            .Property("fontWeight", "700")
            .Property("fontFamily", "Consolas")
            .Property("foreground", "#FFFFFF")
            .Property("background", "#061A2A")
            .Property("horizontalAlignment", "Center")
            .Property("tooltip", "Enter the Experience district");

        var column = GuiBuilders.Column("forge-column")
            .Property("horizontalAlignment", "Center")
            .Property("verticalAlignment", "Center")
            .Property("padding", "28");

        column.Child(
            GuiBuilders.Text("forge-kicker", "THE SINGULARITY WORKSHOP / FORGE")
                .Property("foreground", "#00A8FF")
                .Property("fontSize", "14")
                .Property("fontWeight", "700")
                .Property("fontFamily", "Consolas")
                .Property("margin", "0,0,0,18"));

        column.Child(
            GuiBuilders.Text("forge-title", "THE FORGE")
                .Property("foreground", "#FFFFFF")
                .Property("fontSize", "64")
                .Property("fontWeight", "700")
                .Property("fontFamily", "Consolas")
                .Property("margin", "0,0,0,10"));

        column.Child(
            GuiBuilders.Text("forge-subtitle", "Build it. Enter it. Follow the rabbit hole.")
                .Property("foreground", "#A6BCD0")
                .Property("fontSize", "17")
                .Property("fontFamily", "Consolas")
                .Property("margin", "0,0,0,28"));

        column.Child(portal);

        _root = GuiBuilders.Panel("forge-root")
            .Property("background", "#020711")
            .Child(column)
            .Build();
    }

    public bool Arbitrate(ArbitrationContext context, int roundIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return false;
    }
}
