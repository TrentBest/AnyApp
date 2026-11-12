using TheSingularityWorkshop.Renderer;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class ObserverRepresentationIntegrationTests
{
    [Fact]
    public void IndependentObserversInvalidateOnlyTheirOwnCachedRepresentation()
    {
        var nearObserver = CreateCoordinator("anyapp-issue50-near");
        var distantObserver = CreateCoordinator("anyapp-issue50-distant");

        using (nearObserver)
        using (distantObserver)
        {
            var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

            var nearInitial = nearObserver.Evaluate(
                distance: 0.5, sourceRevision: 12, observerRevision: 4, now);
            var distantInitial = distantObserver.Evaluate(
                distance: 45, sourceRevision: 12, observerRevision: 9, now);

            Assert.Equal(RepresentationTarget.Interactive, nearInitial.ActiveRepresentation);
            Assert.Equal(RepresentationTarget.Semantic, distantInitial.ActiveRepresentation);
            Assert.NotSame(nearInitial.Bucket, distantInitial.Bucket);
            Assert.True(nearInitial.RefreshTicket.HasValue);
            Assert.True(distantInitial.RefreshTicket.HasValue);

            Assert.True(nearInitial.Bucket.TryCompleteRefresh(nearInitial.RefreshTicket!.Value, now));
            Assert.True(distantInitial.Bucket.TryCompleteRefresh(distantInitial.RefreshTicket!.Value, now));

            var nearRevised = nearObserver.Evaluate(
                distance: 0.5, sourceRevision: 12, observerRevision: 5, now.AddMilliseconds(1));
            var distantUnchanged = distantObserver.Evaluate(
                distance: 45, sourceRevision: 12, observerRevision: 9, now.AddMilliseconds(2));

            Assert.True(nearRevised.RefreshRequired);
            Assert.True(nearRevised.Bucket.DirtyReasons.HasFlag(BucketDirtyReason.ObserverChanged));
            Assert.False(distantUnchanged.RefreshRequired);
            Assert.True(distantUnchanged.CanReuseCachedRepresentation);
            Assert.Equal(1, distantUnchanged.Bucket.Metrics.ReuseCount);
            Assert.Equal(12L, nearRevised.Bucket.CachedSourceRevision);
            Assert.Equal(12L, distantUnchanged.Bucket.CachedSourceRevision);
        }
    }

    private static ObserverRepresentationCoordinator CreateCoordinator(string contextName)
    {
        var rules = new[]
        {
            new RepresentationHorizonRule(
                new EventHorizon("Interactive", 1, 10), RepresentationTarget.Interactive),
            new RepresentationHorizonRule(
                new EventHorizon("Near", 5, 8), RepresentationTarget.Near),
            new RepresentationHorizonRule(
                new EventHorizon("Distant", 15, 4), RepresentationTarget.Distant),
            new RepresentationHorizonRule(
                new EventHorizon("Aggregate", 30, 2), RepresentationTarget.Aggregate),
            new RepresentationHorizonRule(
                new EventHorizon("Semantic", 100, 1), RepresentationTarget.Semantic)
        };
        var policy = new ObserverRepresentationPolicy(rules);
        var buckets = rules.Select(rule =>
            new EventHorizonBucket(
                $"bucket-{rule.Horizon.Name}", rule.Horizon, rule.Horizon.UpdatesPerSecond));

        return new ObserverRepresentationCoordinator(policy, buckets, contextName);
    }
}
