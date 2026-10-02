using System.IO;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Keeps the last successfully launched Experience manifest outside the host binary.
/// </summary>
public static class LastManifestStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static string DirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheSingularityWorkshop",
            "AnyApp");

    private static string FilePath => Path.Combine(DirectoryPath, "last-manifest.json");

    public static async Task SaveAsync(
        ExperienceManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Directory.CreateDirectory(DirectoryPath);

        var json = JsonSerializer.Serialize(manifest, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    public static async Task<ExperienceManifest?> TryLoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            var content = await File.ReadAllBytesAsync(FilePath, cancellationToken);
            return ExperienceManifest.Parse(content);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
