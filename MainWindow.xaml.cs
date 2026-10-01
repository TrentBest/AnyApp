using System.Windows;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var manifest = MonikerExperience.CreateManifest();
        var runtime = AnyAppRuntime.Compose(manifest);

        if (manifest.Bundles.Count == 0)
        {
            RootHost.Children.Add(WpfGuiRenderer.Render(
                GuiBuilders.Warning(
                        "no-manifest",
                        "No Experience manifest is loaded. AnyApp is running, but there is no data to render.")
                    .Property("margin", "32")
                    .Build()));
            return;
        }

        if (!runtime.TryGetBundle<MonikerMicroBundle>(
                MonikerMicroBundle.BundleId,
                out var bundle) ||
            bundle is not IAnyAppSurface surface)
        {
            RootHost.Children.Add(WpfGuiRenderer.Render(
                GuiBuilders.Warning(
                        "missing-surface",
                        "The Experience manifest loaded, but its GUI surface is unavailable.")
                    .Property("margin", "32")
                    .Build()));
            return;
        }

        RootHost.Children.Add(WpfGuiRenderer.Render(MonikerExperience.ExecutePresentation(surface.Root)));
    }
}
