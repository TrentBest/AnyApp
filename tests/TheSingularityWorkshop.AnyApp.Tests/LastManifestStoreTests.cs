using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class LastManifestStoreTests
{
    [Fact]
    public async Task PublicationIdentitySurvivesReadAndCanBeCleared()
    {
        var publication = new PublishedExperience(3201, "2.0.0", "ABCDEF0123456789");

        try
        {
            await LastManifestStore.SavePublicationAsync(publication);

            var restored = await LastManifestStore.TryLoadPublicationAsync();

            Assert.Equal(publication, restored);

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
        await Assert.ThrowsAsync<ArgumentException>(() =>
            LastManifestStore.SavePublicationAsync(
                new PublishedExperience(3201, "", "ABCDEF0123456789")));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            LastManifestStore.SavePublicationAsync(
                new PublishedExperience(3201, "2.0.0", "")));
    }
}
