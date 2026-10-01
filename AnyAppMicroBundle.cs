using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

public sealed class AnyAppMicroBundle : IMicroBundle, IAnyAppSurface
{
    public const ulong BundleId = 2111;
    public const string ProviderId = "anyapp-desktop";

    private GuiNode? _root;

    public MicroBundleDescriptor Descriptor { get; } =
        new(BundleId, "1.0.0", providers: [new MicroBundleProvider(ProviderId)]);

    public ulong Id => Descriptor.Id;
    public IReadOnlyList<BundleRequest> Dependencies => [];

    public GuiNode Root =>
        _root ?? throw new InvalidOperationException(
            "The AnyApp GUI surface is not available until FSM_COS has loaded the bundle.");

    public void Load(MicroBundleLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _root = GuiBuilders.Column("anyapp-root")
            .Property("margin", "32")
            .Property("background", "#101820")
            .Child(GuiBuilders.Text("title", "AnyApp")
                .Property("foreground", "#E8F1F8"))
            .Child(GuiBuilders.Text(
                    "subtitle",
                    "Desktop host • FSM_COS composition • GUI.WPF manifestation")
                .Property("margin", "0,8,0,24")
                .Property("foreground", "#AFC4D4"))
            .Child(GuiBuilders.Text(
                    "status",
                    "RuntimeAssembly composed successfully.")
                .Property("foreground", "#CFE8D5"))
            .Build();
    }

    public bool Arbitrate(ArbitrationContext context, int roundIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return false;
    }
}

public interface IAnyAppSurface
{
    GuiNode Root { get; }
}