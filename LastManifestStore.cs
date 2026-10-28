using System.Security.Cryptography;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Crash-safe local cache for the last successfully launched Experience.
/// Each save writes a complete generation and atomically promotes its pointer.
/// </summary>
public static class LastManifestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ActiveGenerationFileName = "active-generation.txt";

    private static string DefaultRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TheSingularityWorkshop", "AnyApp");

    public static Task SaveAsync(ExperienceManifest manifest, CancellationToken cancellationToken = default) =>
        SaveAsync(manifest, DefaultRoot, cancellationToken);

    /// <summary>Saves into an explicitly selected root, primarily for isolated hosts and tests.</summary>
    public static async Task SaveAsync(
        ExperienceManifest manifest, string rootDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var root = Path.GetFullPath(rootDirectory);
        var generations = Path.Combine(root, "generations");
        Directory.CreateDirectory(generations);
        var generationName = Guid.NewGuid().ToString("N");
        var generationPath = Path.Combine(generations, generationName);
        var configurationDirectory = Path.Combine(generationPath, "configuration");
        Directory.CreateDirectory(configurationDirectory);

        try
        {
            var source = manifest.CreateConfigurationSource();
            var cachedBundles = new List<ExperienceBundleRequest>(manifest.Bundles.Count);
            foreach (var bundle in manifest.Bundles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!source.TryGetConfiguration(
                    manifest.RuntimeId, bundle.BundleId,
                    string.IsNullOrWhiteSpace(bundle.BundleVersion)
                        ? string.IsNullOrWhiteSpace(bundle.ArtifactVersion) ? "1.0.0" : bundle.ArtifactVersion
                        : bundle.BundleVersion,
                    out var configuration))
                {
                    cachedBundles.Add(bundle with { ConfigurationBase64 = "", ConfigurationPath = null });
                    continue;
                }

                var fileName = $"{bundle.BundleId}.bin";
                await File.WriteAllBytesAsync(
                    Path.Combine(configurationDirectory, fileName), configuration.ToArray(), cancellationToken)
                    .ConfigureAwait(false);
                cachedBundles.Add(bundle with
                {
                    ConfigurationBase64 = "",
                    ConfigurationPath = Path.Combine("configuration", fileName)
                        .Replace(Path.DirectorySeparatorChar, '/')
                });
            }

            var cachedManifest = manifest with { Bundles = cachedBundles };
            var json = JsonSerializer.Serialize(cachedManifest, JsonOptions);
            await File.WriteAllTextAsync(
                Path.Combine(generationPath, "manifest.json"), json, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            // The new generation is complete before this single pointer is promoted.
            await WriteAtomicallyAsync(
                Path.Combine(root, ActiveGenerationFileName), generationName, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            TryDeleteDirectory(generationPath);
            throw;
        }
    }

    public static Task SavePublicationAsync(
        PublishedExperience publication, CancellationToken cancellationToken = default) =>
        SavePublicationAsync(publication, DefaultRoot, cancellationToken);

    public static async Task SavePublicationAsync(
        PublishedExperience publication, string rootDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (string.IsNullOrWhiteSpace(publication.Version) || string.IsNullOrWhiteSpace(publication.ContentHash))
            throw new ArgumentException("A publication version and content hash are required.", nameof(publication));

        var manifestPath = ResolveActiveManifestPath(rootDirectory);
        if (manifestPath is null || !File.Exists(manifestPath))
            throw new InvalidOperationException("A cached manifest must be saved before its publication identity.");

        var bytes = await File.ReadAllBytesAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var record = new PublicationRecord(publication, hash);
        var publicationPath = GetPublicationPath(manifestPath);
        await WriteAtomicallyAsync(
            publicationPath, JsonSerializer.Serialize(record, JsonOptions), cancellationToken).ConfigureAwait(false);
    }

    public static Task<PublishedExperience?> TryLoadPublicationAsync(CancellationToken cancellationToken = default) =>
        TryLoadPublicationAsync(DefaultRoot, cancellationToken);

    public static async Task<PublishedExperience?> TryLoadPublicationAsync(
        string rootDirectory, CancellationToken cancellationToken = default)
    {
        var manifestPath = ResolveActiveManifestPath(rootDirectory);
        if (manifestPath is null)
            return null;
        var publicationPath = GetPublicationPath(manifestPath);
        if (!File.Exists(publicationPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(publicationPath, cancellationToken).ConfigureAwait(false);
            var record = JsonSerializer.Deserialize<PublicationRecord>(json, JsonOptions);
            if (record?.Publication is not { } publication ||
                string.IsNullOrWhiteSpace(publication.Version) ||
                string.IsNullOrWhiteSpace(publication.ContentHash) ||
                string.IsNullOrWhiteSpace(record.CachedManifestSha256) ||
                !File.Exists(manifestPath))
                return null;

            var bytes = await File.ReadAllBytesAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return string.Equals(actualHash, record.CachedManifestSha256, StringComparison.OrdinalIgnoreCase)
                ? publication : null;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public static Task ClearPublicationAsync(CancellationToken cancellationToken = default) =>
        ClearPublicationAsync(DefaultRoot, cancellationToken);

    public static Task ClearPublicationAsync(string rootDirectory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifestPath = ResolveActiveManifestPath(rootDirectory);
        if (manifestPath is not null)
        {
            var publicationPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, "publication.json");
            if (File.Exists(publicationPath))
                File.Delete(publicationPath);
        }
        return Task.CompletedTask;
    }

    public static Task<ExperienceManifest?> TryLoadAsync(CancellationToken cancellationToken = default) =>
        TryLoadAsync(DefaultRoot, cancellationToken);

    public static async Task<ExperienceManifest?> TryLoadAsync(
        string rootDirectory, CancellationToken cancellationToken = default)
    {
        var manifestPath = ResolveActiveManifestPath(rootDirectory);
        if (manifestPath is null || !File.Exists(manifestPath))
            return null;

        try
        {
            var generationPath = Path.GetDirectoryName(manifestPath)!;
            var publicationPath = GetPublicationPath(manifestPath);
            if (File.Exists(publicationPath) &&
                await TryLoadPublicationAsync(rootDirectory, cancellationToken).ConfigureAwait(false) is null)
                return null;
            return ExperienceManifest.LoadFile(manifestPath);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static string? ResolveActiveManifestPath(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var root = Path.GetFullPath(rootDirectory);
        var pointerPath = Path.Combine(root, ActiveGenerationFileName);
        if (File.Exists(pointerPath))
        {
            var generationName = File.ReadAllText(pointerPath).Trim();
            if (generationName.Length == 0 ||
                generationName != Path.GetFileName(generationName) ||
                generationName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return null;
            return Path.Combine(root, "generations", generationName, "manifest.json");
        }

        // Backward compatibility for installations using the original flat cache.
        var legacyPath = Path.Combine(root, "last-manifest.json");
        return File.Exists(legacyPath) ? legacyPath : null;
    }

    private static string GetPublicationPath(string manifestPath) =>
        string.Equals(Path.GetFileName(manifestPath), "manifest.json", StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Path.GetDirectoryName(manifestPath)!, "publication.json")
            : Path.Combine(Path.GetDirectoryName(manifestPath)!, "last-publication.json");

    private sealed record PublicationRecord(PublishedExperience Publication, string CachedManifestSha256);

    private static async Task WriteAtomicallyAsync(
        string path, string contents, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
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

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
