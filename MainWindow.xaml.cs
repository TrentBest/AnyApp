using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.Workshop.Gui;

namespace TheSingularityWorkshop.AnyApp;

public partial class MainWindow : Window
{
    private static readonly Uri DefaultRepositoryEndpoint = new("http://localhost:5000/");
    private readonly ListBox _experienceList = new();
    private readonly Button _launchButton = new();
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

            _experienceList.ItemsSource = experiences;
            _experienceList.DisplayMemberPath = nameof(PublishedExperience.ExperienceId);
            _launchButton.IsEnabled = experiences.Count > 0;

            _status.Text = experiences.Count == 0
                ? "No published Experiences are available."
                : $"{experiences.Count} published Experience(s) available.";
        }
        catch (HttpRequestException)
        {
            _status.Text = $"Experience Browser endpoint is unavailable: {endpoint}";
        }
        catch (Exception ex)
        {
            _status.Text = $"Experience Browser failed: {ex.Message}";
        }
    }

    private async void LaunchSelectedExperience(object sender, RoutedEventArgs e)
    {
        if (_experienceList.SelectedItem is not PublishedExperience selected)
            return;

        try
        {
            _launchButton.IsEnabled = false;
            _status.Text = $"Loading Experience {selected.ExperienceId}...";

            using var client = new ExperienceCatalogClient(GetRepositoryEndpoint());
            var manifest = await client.GetManifestAsync(selected);
            LaunchManifest(manifest);
        }
        catch (Exception ex)
        {
            _launchButton.IsEnabled = true;
            _status.Text = $"Unable to launch the selected Experience: {ex.Message}";
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

        var panel = new StackPanel
        {
            Margin = new Thickness(32),
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "AnyApp — Experience Browser",
            FontSize = 32,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 12)
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Select an available Experience to compose and run.",
            FontSize = 16,
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 20)
        });

        _experienceList.MinHeight = 140;
        _experienceList.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(_experienceList);

        _launchButton.Content = "Run Selected Experience";
        _launchButton.Padding = new Thickness(18, 8, 18, 8);
        _launchButton.HorizontalAlignment = HorizontalAlignment.Left;
        _launchButton.Click -= LaunchSelectedExperience;
        _launchButton.Click += LaunchSelectedExperience;
        panel.Children.Add(_launchButton);

        _status.Text = "Contacting the Experience Browser endpoint...";
        _status.Foreground = Brushes.LightGray;
        _status.Margin = new Thickness(0, 16, 0, 0);
        panel.Children.Add(_status);

        RootHost.Children.Add(panel);
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
