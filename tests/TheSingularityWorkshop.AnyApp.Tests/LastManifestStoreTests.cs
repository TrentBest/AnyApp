using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class LastManifestStoreTests
{
    [Fact]
    public async Task PublicationIdentitySurvivesReadAndTamperedManifestIsRejected()
    {
        var root = CreateTestRoot();
        try
        {
            var manifest = new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>());
            var publication = new PublishedExperience(3201, "2.0.0", "ABCDEF0123456789");

            await LastManifestStore.SaveAsync(manifest, root);
            await LastManifestStore.SavePublicationAsync(publication, root);
            Assert.Equal(publication, await LastManifestStore.TryLoadPublicationAsync(root));
            Assert.NotNull(await LastManifestStore.TryLoadAsync(root));

            var manifestPath = GetActiveManifestPath(root);
            await File.AppendAllTextAsync(manifestPath, " ");
            Assert.Null(await LastManifestStore.TryLoadPublicationAsync(root));
            Assert.Null(await LastManifestStore.TryLoadAsync(root));

            await LastManifestStore.ClearPublicationAsync(root);
            Assert.Null(await LastManifestStore.TryLoadPublicationAsync(root));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task PublicationIdentityRejectsMissingImmutableFields()
    {
        var root = CreateTestRoot();
        try
        {
            await LastManifestStore.SaveAsync(
                new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>()), root);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                LastManifestStore.SavePublicationAsync(
                    new PublishedExperience(3201, "", "ABCDEF0123456789"), root));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                LastManifestStore.SavePublicationAsync(
                    new PublishedExperience(3201, "2.0.0", ""), root));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task CancelledSavePreservesPreviouslyPromotedGeneration()
    {
        var root = CreateTestRoot();
        try
        {
            var original = new ExperienceManifest(3201, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
            await LastManifestStore.SaveAsync(original, root);
            var originalPointer = await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt"));

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                LastManifestStore.SaveAsync(
                    new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>()),
                    root, cancellation.Token));

            Assert.Equal(originalPointer, await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt")));
            Assert.Equal("1.0.0", (await LastManifestStore.TryLoadAsync(root))?.Version);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task SavePromotesNewGenerationWithoutOverwritingOldGeneration()
    {
        var root = CreateTestRoot();
        try
        {
            await LastManifestStore.SaveAsync(
                new ExperienceManifest(3201, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>()), root);
            var oldPointer = await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt"));

            await LastManifestStore.SaveAsync(
                new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>()), root);
            var newPointer = await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt"));

            Assert.NotEqual(oldPointer, newPointer);
            Assert.True(File.Exists(Path.Combine(root, "generations", oldPointer, "manifest.json")));
            Assert.Equal("2.0.0", (await LastManifestStore.TryLoadAsync(root))?.Version);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task ManifestAndPublicationArePromotedTogether()
    {
        var root = CreateTestRoot();
        try
        {
            var manifest = new ExperienceManifest(3201, "3.0.0", 1, Array.Empty<ExperienceBundleRequest>());
            var publication = new PublishedExperience(3201, "3.0.0", "FEDCBA9876543210");
            await LastManifestStore.SaveAsync(manifest, publication, root);

            Assert.Equal("3.0.0", (await LastManifestStore.TryLoadAsync(root))?.Version);
            Assert.Equal(publication, await LastManifestStore.TryLoadPublicationAsync(root));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task PublicationIdentityMustMatchActiveManifest()
    {
        var root = CreateTestRoot();
        try
        {
            var manifest = new ExperienceManifest(3201, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
            await LastManifestStore.SaveAsync(manifest, root);
            var activePointer = await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                LastManifestStore.SavePublicationAsync(
                    new PublishedExperience(3201, "2.0.0", "ABCDEF0123456789"), root));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                LastManifestStore.SaveAsync(
                    manifest,
                    new PublishedExperience(3201, "2.0.0", "ABCDEF0123456789"),
                    root));

            Assert.Equal(activePointer, await File.ReadAllTextAsync(Path.Combine(root, "active-generation.txt")));
            Assert.Equal("1.0.0", (await LastManifestStore.TryLoadAsync(root))?.Version);
            Assert.Null(await LastManifestStore.TryLoadPublicationAsync(root));
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public async Task LegacyFlatManifestCanStillBeLoaded()
    {
        var root = CreateTestRoot();
        try
        {
            Directory.CreateDirectory(root);
            var legacy = new ExperienceManifest(3201, "1.2.3", 1, Array.Empty<ExperienceBundleRequest>());
            await File.WriteAllTextAsync(
                Path.Combine(root, "last-manifest.json"),
                System.Text.Json.JsonSerializer.Serialize(legacy,
                    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));

            Assert.Equal("1.2.3", (await LastManifestStore.TryLoadAsync(root))?.Version);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    private static string CreateTestRoot() =>
        Path.Combine(Path.GetTempPath(), "AnyApp-LastManifestStoreTests", Guid.NewGuid().ToString("N"));

    private static string GetActiveManifestPath(string root)
    {
        var generation = File.ReadAllText(Path.Combine(root, "active-generation.txt")).Trim();
        return Path.Combine(root, "generations", generation, "manifest.json");
    }

    private static void DeleteTestRoot(string root)
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }
}
