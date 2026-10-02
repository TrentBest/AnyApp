using Microsoft.Win32;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppProtocolRegistration
{
    private const string Scheme = "anyapp";

    public static void Register()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
            return;

        using var scheme = Registry.CurrentUser.CreateSubKey(
            $@"Software\Classes\{Scheme}");

        scheme?.SetValue("", "URL:AnyApp Experience");
        scheme?.SetValue("URL Protocol", "");

        using var command = scheme?.CreateSubKey(@"shell\open\command");
        command?.SetValue("", $"\"{executable.Replace("\"", "\\\"")}\" \"%1\"");
    }
}
