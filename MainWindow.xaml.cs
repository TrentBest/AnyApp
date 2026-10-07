using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TheSingularityWorkshop.FSM_COS;
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

        await StartDefaultExperienceAsync();
    }

    private async Task StartDefaultExperienceAsync()
    {
        ExperienceManifest? lastManifest = null;

        try
        {
            lastManifest = await LastManifestStore.TryLoadAsync();
        }
        catch
        {
            // A damaged local last-run record must never prevent startup.
        }

        // The Workshop Moniker is the common first manifestation. It is not a
        // host-local splash or an application-specific page: it is the first
        // configured Experience surface, after which the user can enter other
        // Experiences through the shell.
        await LaunchMonikerAsync();

        _ = lastManifest;
    }

    private async Task BrowseExperiencesAsync(Uri endpoint)
    {
        ShowBrowser();

        try
        {
            using var client = new ExperienceCatalogClient(endpoint);
            var experiences = await client.ListAsync();

            if (experiences.Count == 0)
            {
                await LaunchForgeAsync();
                return;
            }

            BuildExperienceDoors(experiences);

            _status.Text =
                $"{experiences.Count} Experience door(s) are open.";
        }
        catch (HttpRequestException)
        {
            await LaunchForgeAsync();
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
            _experienceDoors.Children.Add(CreateExperienceDoor(experience));
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

        var deepDive = new Button
        {
            Content = "DEEP DIVE  ↗",
            Margin = new Thickness(0, 14, 0, 0),
            Padding = new Thickness(10, 5, 10, 5),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(Color.FromRgb(2, 12, 22)),
            Foreground = accent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 68, 92)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = "Open this Experience's browser-only Workshop Deep Dive"
        };
        deepDive.Click += (_, _) =>
        {
            if (!DeepDiveBrowserLauncher.TryOpen(experience.ExperienceId, out var error))
                _status.Text = error ?? "The Workshop Deep Dive could not be opened.";
        };
        content.Children.Add(deepDive);

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
            _status.Text =
                $"Opening Experience {selected.ExperienceId} {selected.Version}...";
            SetDoorsEnabled(false);

            using var client = new ExperienceCatalogClient(GetRepositoryEndpoint());
            var manifest = await client.GetManifestAsync(selected);
            await LaunchManifestAsync(manifest, persistLastManifest: true);
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
            await LaunchManifestAsync(
                ExperienceManifest.Parse(content),
                persistLastManifest: true);
        }
        catch (Exception ex)
        {
            ShowMessage("Experience manifest could not be loaded.", ex.Message);
        }
    }

    private async Task LaunchManifestAsync(
        ExperienceManifest manifest,
        bool persistLastManifest,
        bool showStartupSplash = false)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var compositionTask = AnyAppRuntime.ComposeAsync(
            manifest,
            GetRepositoryEndpoint());

        if (showStartupSplash)
            await StartupSplash.PresentAsync(RootHost, compositionTask);

        var runtime = await compositionTask;
        RenderRuntime(manifest, runtime);

        if (persistLastManifest &&
            manifest.ExperienceId != ForgeMicroBundle.ExperienceId)
        {
            await LastManifestStore.SaveAsync(manifest);
        }
    }

    private void RenderRuntime(ExperienceManifest manifest, RuntimeAssembly runtime)
    {
        if (manifest.Bundles.Count == 0)
        {
            ShowMessage(
                "The selected Experience contains no MicroBundle requests.",
                $"Experience {manifest.ExperienceId} {manifest.Version} produced an empty runtime manifest.");
            return;
        }

        var bundleId = manifest.Bundles[0].BundleId;

        if (bundleId == MonikerMicroBundle.BundleId &&
            runtime.TryGetBundle<MonikerMicroBundle>(
                MonikerMicroBundle.BundleId,
                out var monikerBundle) &&
            monikerBundle is IAnyAppSurface monikerSurface)
        {
            RootHost.Children.Clear();
            RootHost.Children.Add(
                AnyAppMonikerShell.Create(
                    MonikerExperience.ExecutePresentation(monikerSurface.Root),
                    action =>
                    {
                        if (string.Equals(action, "CREATE", StringComparison.Ordinal))
                            _ = LaunchForgeAsync();
                        else if (string.Equals(action, "EXPERIENCES", StringComparison.Ordinal))
                            _ = BrowseExperiencesAsync(GetRepositoryEndpoint());
                    }));
            return;
        }

        if (bundleId == ForgeMicroBundle.BundleId &&
            runtime.TryGetBundle<ForgeMicroBundle>(
                ForgeMicroBundle.BundleId,
                out var forgeBundle) &&
            forgeBundle is IAnyAppSurface forgeSurface)
        {
            RootHost.Children.Clear();
            var rendered = WpfGuiRenderer.Render(forgeSurface.Root);
            RootHost.Children.Add(rendered);
            WireForgeNavigation(rendered);
            return;
        }

        ShowMessage(
            "The selected Experience could not be manifested.",
            $"MicroBundle {bundleId} is not available in the native host catalog.");
    }

    private void WireForgeNavigation(FrameworkElement root)
    {
        if (FindNamedElement(root, "forgeexplore") is Button explore)
        {
            explore.Click += async (_, _) =>
                await BrowseExperiencesAsync(GetRepositoryEndpoint());
        }
    }

    private static FrameworkElement? FindNamedElement(
        DependencyObject root,
        string name)
    {
        if (root is FrameworkElement element &&
            string.Equals(element.Name, name, StringComparison.Ordinal))
            return element;

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            var match = FindNamedElement(child, name);
            if (match is not null)
                return match;
        }

        return null;
    }

    private async Task LaunchMonikerAsync()
    {
        try
        {
            var path = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "experiences",
                "moniker",
                "1.0.0",
                "manifest.json");

            var content = await File.ReadAllBytesAsync(path);
            await LaunchManifestAsync(
                ExperienceManifest.Parse(content),
                persistLastManifest: false,
                showStartupSplash: false);
        }
        catch (Exception ex)
        {
            ShowMessage(
                "The Workshop Moniker could not be opened.",
                ex.Message);
        }
    }

    private async Task LaunchForgeAsync(bool showStartupSplash = false)
    {
        try
        {
            var path = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "experiences",
                "forge",
                "1.0.0",
                "manifest.json");

            var content = await File.ReadAllBytesAsync(path);
            await LaunchManifestAsync(
                ExperienceManifest.Parse(content),
                persistLastManifest: false,
                showStartupSplash: showStartupSplash);
        }
        catch (Exception ex)
        {
            ShowMessage(
                "The Forge could not be opened.",
                ex.Message);
        }
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

        var hero = new Grid
        {
            Margin = new Thickness(0, 0, 0, 8)
        };

        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        hero.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var avatar = CreateWorkshopAvatar();
        Grid.SetColumn(avatar, 0);
        hero.Children.Add(avatar);

        var heading = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        };

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
            Text = "You are here. Experiences are places you enter, not applications you launch.",
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(166, 188, 207))
        });

        Grid.SetColumn(heading, 1);
        hero.Children.Add(heading);

        Grid.SetRow(hero, 0);
        shell.Children.Add(hero);

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

    private static Grid CreateWorkshopAvatar()
    {
        const string source =
            "https://raw.githubusercontent.com/TrentBest/WebPage/master/" +
            "TheSingularityWorkshop/wwwroot/Images/TheSingularityWorkshopLogo.png";

        var avatar = new Grid
        {
            Width = 132,
            Height = 132,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };

        var image = new BitmapImage(new Uri(source, UriKind.Absolute));
        var portal = new Ellipse
        {
            Width = 112,
            Height = 112,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Stroke = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
            StrokeThickness = 2,
            Fill = new ImageBrush(image)
            {
                Stretch = Stretch.UniformToFill
            },
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.7,
                Color = Color.FromRgb(0, 234, 255)
            },
            ToolTip = "Your Workshop avatar"
        };

        var you = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.FromRgb(2, 7, 17)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 168, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(7, 3, 7, 3),
            Child = new TextBlock
            {
                Text = "YOU",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 234, 255))
            }
        };

        avatar.Children.Add(portal);
        avatar.Children.Add(you);
        return avatar;
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
            : System.IO.Path.GetFullPath(argument[prefix.Length..]);
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
