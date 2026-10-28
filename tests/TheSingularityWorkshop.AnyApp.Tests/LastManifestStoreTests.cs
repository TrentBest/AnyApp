using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class LastManifestStoreTests
{
    [Fact]
    public async Task PublicationIdentitySurvivesReadAndCanBeCleared()
    {
        var manifest = new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var publication = new PublishedExperience(3201, "2.0.0", "ABCDEF0123456789");

        try
        {
            await LastManifestStore.SaveAsync(manifest);
            await LastManifestStore.SavePublicationAsync(publication);

            var restored = await LastManifestStore.TryLoadPublicationAsync();

            Assert.Equal(publication, restored);

            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TheSingularityWorkshop",
                "AnyApp");
            var cachedManifestPath = Path.Combine(directory, "last-manifest.json");
            await File.AppendAllTextAsync(cachedManifestPath, " ");

            Assert.Null(await LastManifestStore.TryLoadPublicationAsync());
            Assert.Null(await LastManifestStore.TryLoadAsync());

            await LastManifestStore.ClearPublicationAsync();

            Assert.Null(await LastManifestStore.TryLoadPublicationAsync());
        }
        finally
        {
            await LastManifestStore.ClearPublicationAsync();
        }
    }

    [Fact]
    public async Task PublicationIdentityRejectsMissingImmutableFields()
    {
        await LastManifestStore.SaveAsync(
            new ExperienceManifest(3201, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>()));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            LastManifestStore.SavePublicationAsync(
                new PublishedExperience(3201, "", "ABCDEF0123456789")));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            LastManifestStore.SavePublicationAsync(
                new PublishedExperience(3201, "2.0.0", "")));
    }
}
