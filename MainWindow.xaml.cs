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
using TheSingularityWorkshop.Experiences.Moniker;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.Renderer;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;
#if WORKSHOP_STORYBUILDER
using StoryBuilder.MicroBundle;
#endif

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    private static readonly Uri DefaultRepositoryEndpoint = new("http://localhost:5000/");
    private readonly WrapPanel _experienceDoors = new();
    private readonly TextBlock _status = new();
    private readonly AnyAppWebBridge _webBridge = new();
    private AnyAppLivingDemo? _livingDemo;
    private GuiNode? _currentMonikerRoot;
    private IMonikerMotion? _currentMonikerMotion;
    private bool _monikerMotionEnabled;
#if WORKSHOP_STORYBUILDER
    private StoryBuilderAuthoringSurface? _storyBuilderSurface;
#endif

    public MainWindow()
    {
        InitializeComponent();
        AnyAppProtocolRegistration.Register();
        Loaded += OnLoaded;
        Closed += (_, _) => { _livingDemo?.Dispose(); _webBridge.Dispose(); };
        _webBridge.CommandReceived += HandleBridgeCommand;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        var launchRequest = GetLaunchRequest();
        if (launchRequest is not null)
        {
            _webBridge.SetLaunchToken(launchRequest.LaunchToken);
            await LaunchPublishedExperienceAsync(launchRequest);
            return;
        }

        var launchFile = GetLaunchFile();
        if (launchFile is not null)
        {
            await LaunchManifestFileAsync(launchFile);
            return;
        }

#if WORKSHOP_STORYBUILDER
        if (Environment.GetCommandLineArgs().Any(argument =>
                string.Equals(argument, "--storybuilder", StringComparison.OrdinalIgnoreCase)))
        {
            StartStoryBuilder();
            return;
        }
#endif

        if (Environment.GetCommandLineArgs().Any(argument =>
                string.Equals(argument, "--renderer-proof", StringComparison.OrdinalIgnoreCase)))
        {
            RootHost.Children.Clear();
            RootHost.Children.Add(new RendererDesktopProofView());
            return;
        }

        await StartDefaultExperienceAsync();
    }

#if WORKSHOP_STORYBUILDER
    /// <summary>Starts an isolated StoryBuilder authoring session in the native AnyApp host.</summary>
    private void StartStoryBuilder()
    {
        _storyBuilderSurface = new StoryBuilderAuthoringSurface(
            new StoryBuilderAuthoringBundle().CreateSession("AnyApp StoryBuilder"));

        Title = "AnyApp — StoryBuilder";
        RootHost.Children.Clear();
        RootHost.Children.Add(WpfGuiRenderer.Render(
            _storyBuilderSurface.Root,
            HandleStoryBuilderCommand));
    }

    /// <summary>Dispatches only the explicit StoryBuilder commands and refreshes from domain state.</summary>
    private void HandleStoryBuilderCommand(string commandId)
    {
        if (_storyBuilderSurface is null)
            return;

        if (commandId is not (
            StoryBuilderAuthoringCommands.CreateTimeline or
            StoryBuilderAuthoringCommands.CreateCharacter or
            StoryBuilderAuthoringCommands.CreateScene or
            StoryBuilderAuthoringCommands.CreateManuscript))
        {
            Title = $"AnyApp — StoryBuilder — Rejected command: {commandId}";
            return;
        }

        try
        {
            var result = _storyBuilderSurface.Execute(commandId);
            Title = $"AnyApp — StoryBuilder — {result.Message}";

            if (result.Succeeded)
            {
                RootHost.Children.Clear();
                RootHost.Children.Add(WpfGuiRenderer.Render(
                    _storyBuilderSurface.Root,
                    HandleStoryBuilderCommand));
            }
        }
        catch (Exception exception)
        {
            Title = $"AnyApp — StoryBuilder — {exception.Message}";
        }
    }
#endif

    private async Task StartDefaultExperienceAsync()
    {
        // Match WebPage's manifest-first contract: the Moniker is the introduction,
        // not the destination. Resume the last assigned Experience when possible;
        // otherwise enter the Workshop Forge after the Moniker presentation.
        ExperienceManifest? lastManifest = null;
        try
        {
            lastManifest = await LastManifestStore.TryLoadAsync();
        }
        catch
        {
            // A damaged last-run record must not prevent the default entry Experience.
        }

        if (lastManifest is not null)
        {
            try
            {
                await LaunchManifestAsync(lastManifest, persistLastManifest: true);
                return;
            }
            catch
            {
                // A stale/unavailable last Experience falls back to Forge.
            }
        }

        await LaunchForgeAsync();
    }

    private async Task LaunchPublishedExperienceAsync(AnyAppLaunchRequest request)
    {
        try
        {
            using var client = new ExperienceCatalogClient(GetRepositoryEndpoint());
            var manifest = await client.GetManifestAsync(
                new PublishedExperience(
                    request.ExperienceId,
                    request.Version,
                    request.ContentHash));

            if (manifest.ExperienceId != request.ExperienceId ||
                !string.Equals(manifest.Version, request.Version, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The published Experience manifest identity does not match the requested deep link.");
            }

            await LaunchManifestAsync(
                manifest,
                persistLastManifest: manifest.ExperienceId != ForgeMicroBundle.ExperienceId);
        }
        catch (Exception ex)
        {
            ShowMessage(
                "The requested Experience could not be opened.",
                ex.Message);
        }
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
        bool persistLastManifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _webBridge.Configure(manifest);
        _webBridge.Start();
        _webBridge.SetState("composing", false);

        if (manifest.Intent is not null && !WorkshopSemanticExchange.IsAllowed(manifest.Intent))
        {
            throw new InvalidOperationException(
                $"Experience intent '{manifest.Intent.Name}' is not admitted by the host semantic grammar.");
        }

        // Start the destination composition immediately. Compose a small startup
        // manifest in parallel so its Moniker can be presented while the destination
        // loads; the hand-off waits for both the authored presentation window and the
        // destination runtime to be ready.
        var endpoint = GetRepositoryEndpoint();
        var compositionTask = Task.Run(() => AnyAppRuntime.ComposeAsync(manifest, endpoint));

        ExperienceManifest? fallbackMoniker = null;
        if (manifest.GetStartupMonikerBundleIds().Count == 0)
        {
            var monikerPath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "experiences",
                "moniker",
                "development",
                "manifest.json");
            try
            {
                fallbackMoniker = ExperienceManifest.Parse(
                    await File.ReadAllBytesAsync(monikerPath));
            }
            catch
            {
                // A destination can still run if the optional fallback Moniker is absent.
            }
        }

        var startupManifest = manifest.CreateStartupMonikerManifest(fallbackMoniker);
        if (startupManifest is not null)
        {
            var startupTask = Task.Run(() => AnyAppRuntime.ComposeAsync(startupManifest, endpoint));
            var startupRuntime = await startupTask;
            await PresentStartupMonikersAsync(startupManifest, startupRuntime);
        }

        var runtime = await compositionTask;
        await RenderRuntimeAsync(manifest, runtime);

        if (persistLastManifest &&
            manifest.ExperienceId != ForgeMicroBundle.ExperienceId)
        {
            await LastManifestStore.SaveAsync(manifest);
        }
    }

    private async Task PresentStartupMonikersAsync(
        ExperienceManifest manifest,
        RuntimeAssembly runtime)
    {
        _currentMonikerRoot = null;
        _currentMonikerMotion = runtime.TryGetBundle<MonikerWaveMotionMicroBundle>(
            MonikerWaveMotionMicroBundle.BundleId,
            out var motion)
            ? motion
            : null;

        _monikerMotionEnabled = false;
        if (runtime.TryGetBundle<RendererMicroBundle>(
                RendererMicroBundle.BundleId,
                out var renderer) &&
            renderer is { IsLoaded: true })
        {
            using var rendererMachine = new RendererComputationMachine(
                new RendererComputationContext(
                    $"Experience-{manifest.ExperienceId}-Moniker",
                    RepresentationTarget.Interactive));
            var representation = rendererMachine.Advance();
            _monikerMotionEnabled = representation is
                RepresentationTarget.Near or RepresentationTarget.Interactive;
        }

        foreach (var bundleId in manifest.GetStartupMonikerBundleIds())
        {
            if (!runtime.TryGetBundle<IMicroBundle>(bundleId, out var bundle) ||
                bundle is null ||
                !MicroBundleGuiSurface.TryGetRoot(bundle, out var root) ||
                root is null)
            {
                continue;
            }

            _currentMonikerRoot = bundleId == MonikerExperience.MonikerBundleId
                ? root
                : _currentMonikerRoot;

            RootHost.Children.Clear();
            var presentation = bundleId == MonikerExperience.MonikerBundleId
                ? MonikerExperience.ExecutePresentation(
                    root,
                    _currentMonikerMotion,
                    _monikerMotionEnabled)
                : root;
            RootHost.Children.Add(WpfGuiRenderer.Render(presentation));
            _webBridge.SetState("moniker", false);

            // Match WebPage's host-owned startup presentation window per Moniker.
            // The destination composition has already been running in parallel.
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    private async Task RenderRuntimeAsync(
        ExperienceManifest manifest,
        RuntimeAssembly runtime)
    {
        if (manifest.Bundles.Count == 0)
        {
            ShowMessage(
                "The selected Experience contains no MicroBundle requests.",
                $"Experience {manifest.ExperienceId} {manifest.Version} produced an empty runtime manifest.");
            return;
        }

        var startupMonikerIds = manifest.GetStartupMonikerBundleIds().ToHashSet();
        var selectedBundleId = manifest.Bundles
            .Select(bundle => bundle.BundleId)
            .FirstOrDefault(bundleId =>
                !startupMonikerIds.Contains(bundleId) &&
                HasGuiSurface(runtime, bundleId));

        if (selectedBundleId != 0 &&
            runtime.TryGetBundle<IMicroBundle>(
                selectedBundleId,
                out var selectedBundle) &&
            selectedBundle is not null &&
            MicroBundleGuiSurface.TryGetRoot(selectedBundle, out var selectedRoot) &&
            selectedRoot is not null)
        {
            RootHost.Children.Clear();
            var rendered = WpfGuiRenderer.Render(selectedRoot);
            RootHost.Children.Add(rendered);

            if (selectedBundleId == ForgeMicroBundle.BundleId)
                WireForgeNavigation(rendered);

            return;
        }

        if (selectedBundleId == 0)
        {
            await PresentWorkshopGatewayAsync(waitForMoniker: false);
            return;
        }

        ShowMessage(
            "The selected Experience could not be manifested.",
            $"MicroBundle {selectedBundleId} did not expose a semantic GUI surface for the native host.");
    }

    private static bool HasGuiSurface(RuntimeAssembly runtime, ulong bundleId) =>
        runtime.TryGetBundle<IMicroBundle>(bundleId, out var bundle) &&
        bundle is not null &&
        MicroBundleGuiSurface.TryGetRoot(bundle, out var root) &&
        root is not null;

    private void HandleBridgeCommand(string command)
    {
        switch (command.ToLowerInvariant())
        {
            case "return-hub":
                Dispatcher.Invoke(ShowDesktopMoniker);
                break;
            case "split-moniker":
                Dispatcher.Invoke(ShowSplitMoniker);
                break;
            case "whole-moniker":
                Dispatcher.Invoke(ShowDesktopMoniker);
                break;
            case "flip-moniker":
                Dispatcher.Invoke(ShowSplitMoniker);
                break;
        }
    }

    private void ShowSplitMoniker()
    {
        _livingDemo?.Dispose();
        _livingDemo = null;
        RootHost.Children.Clear();

        var shell = new Grid
        {
            Background = new LinearGradientBrush(
                Color.FromRgb(2, 7, 17),
                Color.FromRgb(7, 2, 17),
                90)
        };

        shell.Children.Add(CreateSplitMoniker(_webBridge.DesktopHalf));
        RootHost.Children.Add(shell);
        _webBridge.SetState("moniker-split", true);
    }

    private static FrameworkElement CreateSplitMoniker(string half)
    {
        const double halfWidth = 700;

        var window = new Grid
        {
            Width = halfWidth,
            Height = 300,
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var full = new StackPanel
        {
            Width = halfWidth * 2,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = new TranslateTransform(
                string.Equals(half, "right", StringComparison.OrdinalIgnoreCase) ? -halfWidth : 0,
                0)
        };

        full.Children.Add(CreateMonikerLine("THE", 62, Color.FromRgb(0, 168, 255)));
        full.Children.Add(CreateMonikerLine("SINGULARITY", 74, Color.FromRgb(255, 44, 255)));
        full.Children.Add(CreateMonikerLine("WORKSHOP", 74, Color.FromRgb(255, 122, 0)));

        window.Children.Add(full);
        return window;
    }

    private static TextBlock CreateMonikerLine(string text, double size, Color color)
        => new()
        {
            Text = text,
            Width = 1400,
            TextAlignment = TextAlignment.Center,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            FontSize = size,
            FontWeight = FontWeights.Black,
            Foreground = new SolidColorBrush(color),
            LineHeight = size * .86,
            Margin = new Thickness(0, -4, 0, -4)
        };

    private void ShowDesktopMoniker()
    {
        _livingDemo?.Dispose();
        _livingDemo = null;
        RootHost.Children.Clear();

        if (_currentMonikerRoot is null)
        {
            _ = LaunchMonikerAsync();
            return;
        }

        RootHost.Children.Add(
            WpfGuiRenderer.Render(
                MonikerExperience.ExecutePresentation(_currentMonikerRoot, _currentMonikerMotion, _monikerMotionEnabled)));

        _webBridge.SetState("moniker", false);
    }

    private async Task PresentWorkshopGatewayAsync(bool waitForMoniker = true)
    {
        if (waitForMoniker)
            await Task.Delay(TimeSpan.FromSeconds(3));

        await Dispatcher.InvokeAsync(() =>
        {
            RootHost.Children.Clear();

            var shell = new Grid
            {
                Background = new LinearGradientBrush(
                    Color.FromRgb(2, 7, 17),
                    Color.FromRgb(7, 2, 17),
                    90)
            };

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (_currentMonikerRoot is not null)
            {
                stack.Children.Add(
                    WpfGuiRenderer.Render(
                        MonikerExperience.ExecutePresentation(_currentMonikerRoot, _currentMonikerMotion, _monikerMotionEnabled)));
            }

            var enter = new Button
            {
                Content = "ENTER THE WORKSHOP",
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(34, 16, 34, 16),
                Margin = new Thickness(0, 28, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(4, 18, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 234, 255)),
                BorderThickness = new Thickness(1),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            enter.Click += (_, _) =>
            {
                _livingDemo?.Dispose();
                _livingDemo = AnyAppLivingDemo.Create();
                RootHost.Children.Clear();
                RootHost.Children.Add(_livingDemo);
                _webBridge.SetState("living", true);
            };

            stack.Children.Add(enter);
            shell.Children.Add(stack);
            RootHost.Children.Add(shell);
            _webBridge.SetState("hub", true);
        });
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
                "development",
                "manifest.json");

            var content = await File.ReadAllBytesAsync(path);
            await LaunchManifestAsync(
                ExperienceManifest.Parse(content),
                persistLastManifest: false);
        }
        catch (Exception ex)
        {
            ShowMessage(
                "The Workshop Moniker could not be opened.",
                ex.Message);
        }
    }

    private async Task LaunchForgeAsync()
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
                persistLastManifest: false);
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

    private static AnyAppLaunchRequest? GetLaunchRequest()
    {
        var argument = Environment.GetCommandLineArgs()
            .FirstOrDefault(value =>
                value.StartsWith("anyapp://", StringComparison.OrdinalIgnoreCase));

        return AnyAppLaunchRequest.TryParse(argument);
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
