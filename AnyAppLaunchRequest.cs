namespace TheSingularityWorkshop.AnyApp;

public sealed record AnyAppLaunchRequest(
    ulong ExperienceId,
    string Version,
    string ContentHash,
    string LaunchToken)
{
    public static AnyAppLaunchRequest? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "anyapp", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "experience", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length != 3 ||
            !ulong.TryParse(segments[0], out var experienceId) ||
            string.IsNullOrWhiteSpace(segments[1]) ||
            string.IsNullOrWhiteSpace(segments[2]))
        {
            return null;
        }

        var token = GetQueryValue(uri, "token");

        return string.IsNullOrWhiteSpace(token)
            ? null
            : new AnyAppLaunchRequest(
                experienceId,
                Uri.UnescapeDataString(segments[1]),
                segments[2],
                token);
    }

    private static string? GetQueryValue(Uri uri, string name)
    {
        var query = uri.Query.TrimStart('?');

        foreach (var pair in query.Split(
                     '&',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = Uri.UnescapeDataString(pair[..separator]);
            if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                continue;

            return Uri.UnescapeDataString(pair[(separator + 1)..]);
        }

        return null;
    }
}
