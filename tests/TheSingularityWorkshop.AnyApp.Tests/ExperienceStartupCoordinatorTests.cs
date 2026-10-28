using System.Net;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class ExperienceStartupCoordinatorTests
{
    [Fact]
    public async Task UnchangedPublicationUsesCachedManifestWithoutFetchingArtifact()
    {
        var manifest = new ExperienceManifest(41, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var publication = new PublishedExperience(41, "1.0.0", "abc123");
        var fetchCount = 0;

        var decision = await ExperienceStartupCoordinator.ReconcileAsync(
            _ => Task.FromResult<ExperienceManifest?>(manifest),
            _ => Task.FromResult<PublishedExperience?>(publication),
            _ => Task.FromResult<IReadOnlyList<PublishedExperience>>([publication]),
            (_, _) =>
            {
                fetchCount++;
                return Task.FromResult(manifest);
            });

        Assert.Same(manifest, decision.Manifest);
        Assert.Equal(publication, decision.Publication);
        Assert.True(decision.PersistLastManifest);
        Assert.Equal(0, fetchCount);
    }

    [Fact]
    public async Task ChangedPublicationFetchesAndReturnsNewImmutableManifest()
    {
        var cached = new ExperienceManifest(41, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var updated = new ExperienceManifest(41, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var oldPublication = new PublishedExperience(41, "1.0.0", "old-hash");
        var current = new PublishedExperience(41, "2.0.0", "new-hash");
        var fetchCount = 0;

        var decision = await ExperienceStartupCoordinator.ReconcileAsync(
            _ => Task.FromResult<ExperienceManifest?>(cached),
            _ => Task.FromResult<PublishedExperience?>(oldPublication),
            _ => Task.FromResult<IReadOnlyList<PublishedExperience>>([current]),
            (publication, _) =>
            {
                fetchCount++;
                Assert.Equal(current, publication);
                return Task.FromResult(updated);
            });

        Assert.Same(updated, decision.Manifest);
        Assert.Equal(current, decision.Publication);
        Assert.Equal(1, fetchCount);
        Assert.True(decision.PersistLastManifest);
    }

    [Fact]
    public async Task OfflineCatalogResumesCacheWithoutChangingPublicationIdentity()
    {
        var manifest = new ExperienceManifest(41, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var publication = new PublishedExperience(41, "1.0.0", "abc123");
        var fetchCount = 0;

        var decision = await ExperienceStartupCoordinator.ReconcileAsync(
            _ => Task.FromResult<ExperienceManifest?>(manifest),
            _ => Task.FromResult<PublishedExperience?>(publication),
            _ => Task.FromException<IReadOnlyList<PublishedExperience>>(new HttpRequestException("offline")),
            (_, _) =>
            {
                fetchCount++;
                return Task.FromResult(manifest);
            });

        Assert.Same(manifest, decision.Manifest);
        Assert.Null(decision.Publication);
        Assert.False(decision.PersistLastManifest);
        Assert.Equal(0, fetchCount);
    }

    [Fact]
    public async Task MissingPreviouslyPublishedExperienceLaunchesForge()
    {
        var manifest = new ExperienceManifest(41, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var publication = new PublishedExperience(41, "1.0.0", "abc123");

        var decision = await ExperienceStartupCoordinator.ReconcileAsync(
            _ => Task.FromResult<ExperienceManifest?>(manifest),
            _ => Task.FromResult<PublishedExperience?>(publication),
            _ => Task.FromResult<IReadOnlyList<PublishedExperience>>([]),
            (_, _) => throw new InvalidOperationException("No fetch should be attempted."));

        Assert.True(decision.ShouldLaunchForge);
    }

    [Fact]
    public async Task CatalogIdentityChangeFetchesAndRejectsMismatchedManifest()
    {
        var cached = new ExperienceManifest(41, "1.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var mismatched = new ExperienceManifest(99, "2.0.0", 1, Array.Empty<ExperienceBundleRequest>());
        var oldPublication = new PublishedExperience(41, "1.0.0", "old-hash");
        var current = new PublishedExperience(41, "2.0.0", "new-hash");

        var decision = await ExperienceStartupCoordinator.ReconcileAsync(
            _ => Task.FromResult<ExperienceManifest?>(cached),
            _ => Task.FromResult<PublishedExperience?>(oldPublication),
            _ => Task.FromResult<IReadOnlyList<PublishedExperience>>([current]),
            (_, _) => Task.FromResult(mismatched));

        Assert.True(decision.ShouldLaunchForge);
    }
}
