using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// The first real AnyApp Experience payload: the Workshop Moniker.
/// Content, presentation semantics, and wave parameters live in the MicroBundle;
/// the host only asks GUI.WPF to manifest the resulting semantic tree.
/// </summary>
public sealed class MonikerMicroBundle : IMicroBundle, IAnyAppSurface
{
    public const ulong BundleId = 3101;
    public const string ProviderId = "workshop-moniker";

    private GuiNode? _root;

    public MicroBundleDescriptor Descriptor { get; } =
        new(BundleId, "1.0.0", providers: [new MicroBundleProvider(ProviderId)]);

    public ulong Id => Descriptor.Id;

    public IReadOnlyList<BundleRequest> Dependencies => [];

    public GuiNode Root =>
        _root ?? throw new InvalidOperationException(
            "The Moniker GUI surface is not available until FSM_COS has loaded the bundle.");

    public void Load(MicroBundleLoadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var builder = GuiBuilders.Column("moniker-column")
            .Property("horizontalAlignment", "Center")
            .Property("verticalAlignment", "Center")
            .Property("padding", "24");

        foreach (var (word, offset) in new[]
                 {
                     ("THE", 0),
                     ("SINGULARITY", 3),
                     ("WORKSHOP", 14)
                 })
        {
            builder.Child(BuildWord(word, offset));
        }

        _root = GuiBuilders.Panel("moniker-root")
            .Property("background", "#020711")
            .Child(builder)
            .Build();
    }

    private static GuiBuilder BuildWord(string word, int offset)
    {
        var row = GuiBuilders.Row($"moniker-{word.ToLowerInvariant()}")
            .Property("horizontalAlignment", "Center");

        for (var index = 0; index < word.Length; index++)
        {
            var phase = (offset + index) % 6;
            var color = phase switch
            {
                0 => "#FF3030",
                1 => "#FF7A00",
                2 => "#FFD34D",
                3 => "#52E05A",
                4 => "#00A8FF",
                _ => "#FF2CFF"
            };

            row.Child(
                GuiBuilders.Text(
                        $"moniker-{word.ToLowerInvariant()}-{index}",
                        word[index].ToString())
                    .Property("foreground", color)
                    .Property("fontSize", "76")
                    .Property("fontWeight", "700")
                    .Property("fontFamily", "Consolas")
                    .Property("wavePeriod", "3.8")
                    .Property("waveAmplitude", "7")
                    .Property("waveRotation", "2.5")
                    .Property("wavePhase", ((offset + index) % 12 / 12d).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)));
        }

        return row.Build();
    }

    public bool Arbitrate(ArbitrationContext context, int roundIndex)
    {
        ArgumentNullException.ThrowIfNull(context);
        return false;
    }
}
