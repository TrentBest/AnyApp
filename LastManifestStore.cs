using System.IO;
using System.Security.Cryptography;
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
    private static string PublicationFilePath => Path.Combine(DirectoryPath, "last-publication.json");

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

    /// <summary>Persists the immutable publisher identity associated with the last launched manifest.</summary>
    public static async Task SavePublicationAsync(
        PublishedExperience publication,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (string.IsNullOrWhiteSpace(publication.Version) ||
            string.IsNullOrWhiteSpace(publication.ContentHash))
            throw new ArgumentException("A publication version and content hash are required.", nameof(publication));

        Directory.CreateDirectory(DirectoryPath);
        if (!File.Exists(FilePath))
            throw new InvalidOperationException("A cached manifest must be saved before its publication identity.");

        var cachedManifestBytes = await File.ReadAllBytesAsync(FilePath, cancellationToken).ConfigureAwait(false);
        var cachedManifestHash = Convert.ToHexString(SHA256.HashData(cachedManifestBytes)).ToLowerInvariant();
        var record = new PublicationRecord(publication, cachedManifestHash);
        var json = JsonSerializer.Serialize(record, JsonOptions);
        await WriteAtomicallyAsync(PublicationFilePath, json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Loads the last known publisher identity, or null when no valid identity is available.</summary>
    public static async Task<PublishedExperience?> TryLoadPublicationAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(PublicationFilePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(PublicationFilePath, cancellationToken).ConfigureAwait(false);
            var record = JsonSerializer.Deserialize<PublicationRecord>(json, JsonOptions);
            if (record?.Publication is not { } publication ||
                string.IsNullOrWhiteSpace(publication.Version) ||
                string.IsNullOrWhiteSpace(publication.ContentHash) ||
                string.IsNullOrWhiteSpace(record.CachedManifestSha256) ||
                !File.Exists(FilePath))
                return null;

            var cachedManifestBytes = await File.ReadAllBytesAsync(FilePath, cancellationToken).ConfigureAwait(false);
            var actualHash = Convert.ToHexString(SHA256.HashData(cachedManifestBytes)).ToLowerInvariant();
            return string.Equals(actualHash, record.CachedManifestSha256, StringComparison.OrdinalIgnoreCase)
                ? publication
                : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public static Task ClearPublicationAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(PublicationFilePath))
            File.Delete(PublicationFilePath);
        return Task.CompletedTask;
    }

    private sealed record PublicationRecord(
        PublishedExperience Publication,
        string CachedManifestSha256);

    private static async Task WriteAtomicallyAsync(
        string path,
        string contents,
        CancellationToken cancellationToken)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, contents, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public static async Task<ExperienceManifest?> TryLoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            // If this manifest is associated with a publication, reject it when its
            // persisted bytes no longer match the digest captured at publication save.
            if (File.Exists(PublicationFilePath) &&
                await TryLoadPublicationAsync(cancellationToken).ConfigureAwait(false) is null)
                return null;

            return ExperienceManifest.LoadFile(FilePath);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
