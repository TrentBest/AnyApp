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
        var configurationDirectory = Path.Combine(DirectoryPath, "configuration");
        Directory.CreateDirectory(configurationDirectory);

        // Keep cached configuration as sidecar files too. The last-manifest cache is
        // not the canonical published artifact, but it must preserve the same separation.
        var source = manifest.CreateConfigurationSource();
        var cachedBundles = new List<ExperienceBundleRequest>(manifest.Bundles.Count);
        foreach (var bundle in manifest.Bundles)
        {
            if (!source.TryGetConfiguration(
                    manifest.RuntimeId,
                    bundle.BundleId,
                    string.IsNullOrWhiteSpace(bundle.BundleVersion)
                        ? string.IsNullOrWhiteSpace(bundle.ArtifactVersion) ? "1.0.0" : bundle.ArtifactVersion
                        : bundle.BundleVersion,
                    out var configuration))
            {
                cachedBundles.Add(bundle with { ConfigurationBase64 = "", ConfigurationPath = null });
                continue;
            }

            var fileName = $"{bundle.BundleId}.bin";
            var fullPath = Path.Combine(configurationDirectory, fileName);
            await File.WriteAllBytesAsync(fullPath, configuration.ToArray(), cancellationToken);
            cachedBundles.Add(bundle with
            {
                ConfigurationBase64 = "",
                ConfigurationPath = Path.Combine("configuration", fileName).Replace(Path.DirectorySeparatorChar, '/')
            });
        }

        var cachedManifest = manifest with { Bundles = cachedBundles };
        var json = JsonSerializer.Serialize(cachedManifest, JsonOptions);
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    public static async Task<ExperienceManifest?> TryLoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            return ExperienceManifest.LoadFile(FilePath);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
