using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Host-owned first contact sequence. FSM_COS composes while the authored splash
/// presentation gives the user a visible transition into the assembled Experience.
/// The initial sequence contains one Workshop-authored image.
/// </summary>
public static class StartupSplash
{
    private const string WorkshopAvatar =
        "https://raw.githubusercontent.com/TrentBest/WebPage/master/" +
        "TheSingularityWorkshop/wwwroot/Images/TheSingularityWorkshopLogo.png";

    public static async Task PresentAsync(
        Panel host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(1, 4, 10))
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var image = new Image
        {
            Width = 220,
            Height = 220,
            HorizontalAlignment = HorizontalAlignment.Center,
            Stretch = Stretch.UniformToFill,
            Opacity = 0
        };

        try
        {
            image.Source = new BitmapImage(new Uri(WorkshopAvatar, UriKind.Absolute));
        }
        catch (UriFormatException)
        {
            // The splash remains usable even when the authored image cannot load.
        }

        var ring = new Grid
        {
            Width = 150,
            Height = 150,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 28)
        };

        var track = new Ellipse
        {
            Width = 132,
            Height = 132,
            Stroke = new SolidColorBrush(Color.FromRgb(32, 58, 78)),
            StrokeThickness = 4
        };

        var progress = new Ellipse
        {
            Width = 132,
            Height = 132,
            Stroke = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            StrokeThickness = 4,
            StrokeDashArray = [0.01, 100],
            StrokeDashCap = PenLineCap.Round,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(-90)
        };

        var percent = new TextBlock
        {
            Text = "0%",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 22,
            FontWeight = FontWeights.Bold
        };

        ring.Children.Add(track);
        ring.Children.Add(progress);
        ring.Children.Add(percent);

        stack.Children.Add(ring);
        stack.Children.Add(image);
        overlay.Children.Add(stack);
        host.Children.Clear();
        host.Children.Add(overlay);

        for (var value = 0; value <= 100; value += 5)
        {
            cancellationToken.ThrowIfCancellationRequested();

            percent.Text = $"{value}%";
            progress.StrokeDashArray = [Math.Max(.01, value * 0.8), 100];
            await Task.Delay(18, cancellationToken);
        }

        await FadeAsync(ring, 0, 1, 180, cancellationToken);
        await FadeAsync(image, 0, 1, 420, cancellationToken);
        await Task.Delay(650, cancellationToken);
        await FadeAsync(overlay, 1, 0, 420, cancellationToken);
    }

    private static Task FadeAsync(
        UIElement element,
        double from,
        double to,
        int milliseconds,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(milliseconds)
        };

        animation.Completed += (_, _) => tcs.TrySetResult();
        element.BeginAnimation(UIElement.OpacityProperty, animation);

        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        return tcs.Task;
    }
}
