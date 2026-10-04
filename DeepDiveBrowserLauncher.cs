using System.Diagnostics;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Opens the Workshop WebPage in the user's default browser for browser-only educational surfaces.
/// </summary>
public static class DeepDiveBrowserLauncher
{
    public static bool TryOpen(ulong experienceId, out string? error)
    {
        var baseUrl = GetWebPageBaseUrl();
        if (baseUrl is null)
        {
            error = "Set SINGULARITY_WORKSHOP_WEBPAGE_URL or pass --webpage-url=... to enable the Workshop browser handoff.";
            return false;
        }

        var target = new Uri(
            new Uri(baseUrl, UriKind.Absolute),
            $"deep-dive/{experienceId}");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target.ToString(),
                UseShellExecute = true
            });

            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string? GetWebPageBaseUrl()
    {
        const string prefix = "--webpage-url=";
        var argument = Environment.GetCommandLineArgs().FirstOrDefault(
            value => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        var value = argument is not null
            ? argument[prefix.Length..]
            : Environment.GetEnvironmentVariable("SINGULARITY_WORKSHOP_WEBPAGE_URL");

        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            return null;

        return uri.ToString().TrimEnd('/') + "/";
    }
}
