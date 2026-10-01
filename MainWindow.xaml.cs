using System.Windows;
using TheSingularityWorkshop.GUI.WPF;

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var runtime = AnyAppRuntime.Compose();

        if (!runtime.TryGetBundle<AnyAppMicroBundle>(
                AnyAppMicroBundle.BundleId,
                out var bundle) ||
            bundle is not IAnyAppSurface surface)
        {
            throw new InvalidOperationException(
                "FSM_COS composed the runtime, but the AnyApp GUI surface was not present.");
        }

        RootHost.Children.Add(WpfGuiRenderer.Render(surface.Root));
    }
}
