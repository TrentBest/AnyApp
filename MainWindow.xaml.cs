using System.Windows;
using TheSingularityWorkshop.GUI.WPF;

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var runtime = AnyAppRuntime.Compose();

        if (!runtime.TryGetBundle<IAnyAppSurface>(
                AnyAppMicroBundle.BundleId,
                out var surface) ||
            surface is null)
        {
            throw new InvalidOperationException(
                "FSM_COS composed the runtime, but the AnyApp surface was not present.");
        }

        RootHost.Children.Add(WpfGuiRenderer.Render(surface.Root));
    }
}