using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    private static readonly Uri DefaultRepositoryEndpoint = new("http://localhost:5000/");
    private readonly WrapPanel _experienceDoors = new();
    private readonly TextBlock _status = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        var launchFile = GetLaunchFile();
        if (launchFile is not null)
        {
            await LaunchManifestFileAsync(launchFile);
            return;
        }

        await BrowseExperiencesAsync(GetRepositoryEndpoint());
    }

    private async Task BrowseExperiencesAsync(Uri endpoint)
    {
        ShowBrowser();

        try
        {
            using var client = new ExperienceCatalogClient(endpoint);
            var experiences = await client.ListAsync();

            BuildExperienceDoors(experiences);

            _status.Text = experiences.Count == 0
                ? "The Workshop has no published Experiences yet."
                : $"{experiences.Count} Experience door(s) are open.";
        }
        catch (HttpRequestException)
        {
            ShowRepositoryUnavailable(endpoint);
        }
        catch (Exception ex)
        {
            _status.Text = $"The Experience Browser could not open: {ex.Message}";
        }
    }

    private void BuildExperienceDoors(IReadOnlyList<PublishedExperience> experiences)
    {
        _experienceDoors.Children.Clear();

        foreach (var experience in experiences)
        {
            _experienceDoors.Children.Add(CreateExperienceDoor(experience));
        }
    }

    private Border CreateExperienceDoor(PublishedExperience experience)
    {
        var accent = new SolidColorBrush(Color.FromRgb(0, 168, 255));
        var border = new Border
        {
            Width = 280,
            Height = 190,
            Margin = new Thickness(12),
            CornerRadius = new CornerRadius(6),
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
            BorderThickness = new Thickness(1),
            Background = new LinearGradientBrush(
                Color.FromRgb(5, 14, 25),
                Color.FromRgb(2, 7, 17),
                90),
            Cursor = Cursors.Hand,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
            ToolTip = "Enter this Experience"
        };

        var content = new StackPanel
        {
            Margin = new Thickness(22),
            VerticalAlignment = VerticalAlignment.Center
        };

        content.Children.Add(new TextBlock
        {
            Text = "EXPERIENCE",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = accent,
            Margin = new Thickness(0, 0, 0, 12)
        });

        content.Children.Add(new TextBlock
        {
            Text = $"#{experience.ExperienceId}",
            FontSize = 30,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });

        content.Children.Add(new TextBlock
        {
            Text = $"Version {experience.Version}",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207)),
            Margin = new Thickness(0, 6, 0, 18)
        });

        content.Children.Add(new TextBlock
        {
            Text = "ENTER  →",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });

        border.Child = content;
        border.MouseEnter += (_, _) => AnimateDoor(border, 1.035, true);
        border.MouseLeave += (_, _) => AnimateDoor(border, 1, false);
        border.MouseLeftButtonUp += (_, _) => _ = EnterExperienceAsync(experience);

        return border;
    }

    private static void AnimateDoor(Border door, double scale, bool active)
    {
        if (door.RenderTransform is not ScaleTransform transform)
            return;

        var animation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(140),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);

        door.BorderThickness = new Thickness(active ? 2 : 1);
        door.BorderBrush = active
            ? new SolidColorBrush(Color.FromRgb(0, 168, 255))
            : new SolidColorBrush(Color.FromRgb(38, 68, 92));
    }

    private async Task EnterExperienceAsync(PublishedExperience selected)
    {
        try
        {
            _status.Text = $"Opening Experience {selected.ExperienceId} {selected.Version}...";
            SetDoorsEnabled(false);

            using var client = new ExperienceCatalogClient(GetRepositoryEndpoint());
            var manifest = await client.GetManifestAsync(selected);
            LaunchManifest(manifest);
        }
        catch (Exception ex)
        {
            SetDoorsEnabled(true);
            _status.Text = $"The Experience could not be entered: {ex.Message}";
        }
    }

    private void SetDoorsEnabled(bool enabled)
    {
        foreach (var child in _experienceDoors.Children)
        {
            if (child is UIElement element)
                element.IsHitTestVisible = enabled;
        }
    }

    private async Task LaunchManifestFileAsync(string path)
    {
        try
        {
            var content = await File.ReadAllBytesAsync(path);
            LaunchManifest(ExperienceManifest.Parse(content));
        }
        catch (Exception ex)
        {
            ShowMessage("Experience manifest could not be loaded.", ex.Message);
        }
    }

    private void LaunchManifest(ExperienceManifest manifest)
    {
        var runtime = AnyAppRuntime.Compose(manifest.ToRuntimeManifest());

        if (manifest.Bundles.Count == 0)
        {
            ShowMessage(
                "The selected Experience contains no MicroBundle requests.",
                $"Experience {manifest.ExperienceId} {manifest.Version} produced an empty runtime manifest.");
            return;
        }

        if (!runtime.TryGetBundle<MonikerMicroBundle>(
                MonikerMicroBundle.BundleId,
                out var bundle) ||
            bundle is not IAnyAppSurface surface)
        {
            ShowMessage(
                "The selected Experience could not be manifested.",
                "The requested MicroBundle is not available in the AnyApp host catalog.");
            return;
        }

        RootHost.Children.Clear();
        RootHost.Children.Add(
            WpfGuiRenderer.Render(
                MonikerExperience.ExecutePresentation(surface.Root)));
    }

    private void ShowBrowser()
    {
        RootHost.Children.Clear();
        RootHost.Background = new LinearGradientBrush(
            Color.FromRgb(1, 4, 10),
            Color.FromRgb(3, 12, 24),
            90);

        var shell = new Grid
        {
            Margin = new Thickness(42, 34, 42, 28)
        };

        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel();

        heading.Children.Add(new TextBlock
        {
            Text = "THE SINGULARITY WORKSHOP",
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 168, 255)),
        });

        heading.Children.Add(new TextBlock
        {
            Text = "Choose a door.",
            FontSize = 38,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 8, 0, 4)
        });

        heading.Children.Add(new TextBlock
        {
            Text = "Experiences are places you enter, not applications you launch.",
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207))
        });

        Grid.SetRow(heading, 0);
        shell.Children.Add(heading);

        _experienceDoors.HorizontalAlignment = HorizontalAlignment.Left;
        _experienceDoors.VerticalAlignment = VerticalAlignment.Center;
        _experienceDoors.Margin = new Thickness(-12, 18, -12, 18);
        Grid.SetRow(_experienceDoors, 1);
        shell.Children.Add(_experienceDoors);

        _status.Text = "Opening the Experience repository...";
        _status.FontSize = 12;
        _status.Foreground = new SolidColorBrush(Color.FromRgb(128, 151, 171));
        Grid.SetRow(_status, 2);
        shell.Children.Add(_status);

        RootHost.Children.Add(shell);
    }

    private void ShowRepositoryUnavailable(Uri endpoint)
    {
        _experienceDoors.Children.Clear();

        var gate = new Border
        {
            Width = 420,
            Height = 170,
            Margin = new Thickness(12),
            BorderBrush = new SolidColorBrush(Color.FromRgb(65, 78, 92)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(5, 10, 18))
        };

        gate.Child = new StackPanel
        {
            Margin = new Thickness(24),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "THE DOORS ARE CLOSED",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "No Experience repository is connected.",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207)),
                    Margin = new Thickness(0, 10, 0, 4)
                },
                new TextBlock
                {
                    Text = endpoint.ToString(),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 151, 171))
                }
            }
        };

        _experienceDoors.Children.Add(gate);
        _status.Text = "Connect the Experience repository to populate the Workshop doors.";
    }

    private void ShowMessage(string title, string detail)
    {
        RootHost.Children.Clear();
        RootHost.Children.Add(
            WpfGuiRenderer.Render(
                GuiBuilders.Warning("experience-launch", $"{title} {detail}")
                    .Property("margin", "32")
                    .Build()));
    }

    private static string? GetLaunchFile()
    {
        const string prefix = "--experience-file=";
        var argument = Environment.GetCommandLineArgs().FirstOrDefault(
            value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return argument is null
            ? null
            : Path.GetFullPath(argument[prefix.Length..]);
    }

    private static Uri GetRepositoryEndpoint()
    {
        const string prefix = "--repository-endpoint=";
        var argument = Environment.GetCommandLineArgs().FirstOrDefault(
            value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return argument is not null &&
               Uri.TryCreate(argument[prefix.Length..], UriKind.Absolute, out var endpoint)
            ? endpoint
            : DefaultRepositoryEndpoint;
    }
}
