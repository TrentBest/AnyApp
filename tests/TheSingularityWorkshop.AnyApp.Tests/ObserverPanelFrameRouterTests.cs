using System.Numerics;
using TheSingularityWorkshop.Renderer;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class ObserverPanelFrameRouterTests
{
    [Fact]
    public void OneObserverFrameCanFanOutToTwoPanelsWithoutReevaluation()
    {
        var frame = CreateFrame();
        var envelope = new ObserverFrameEnvelope(
            "observer-main", observerRevision: 4, sourceRevision: 12, frameSequence: 8, frame);
        var router = new ObserverPanelFrameRouter();

        router.Route(envelope, ["panel-left", "panel-right"]);

        Assert.True(router.TryGetFrame("panel-left", out var left));
        Assert.True(router.TryGetFrame("panel-right", out var right));
        Assert.Same(envelope, left);
        Assert.Same(envelope, right);
        Assert.Same(frame, left!.Frame);
        Assert.Equal(8, right!.FrameSequence);
    }

    [Fact]
    public void DifferentObserversRetainIndependentFrameProvenance()
    {
        var router = new ObserverPanelFrameRouter();
        var near = new ObserverFrameEnvelope(
            "observer-near", observerRevision: 3, sourceRevision: 12, frameSequence: 20, CreateFrame());
        var distant = new ObserverFrameEnvelope(
            "observer-distant", observerRevision: 9, sourceRevision: 12, frameSequence: 21, CreateFrame());

        router.Route(near, ["near-panel"]);
        router.Route(distant, ["distant-panel"]);

        Assert.True(router.TryGetFrame("near-panel", out var nearResult));
        Assert.True(router.TryGetFrame("distant-panel", out var distantResult));
        Assert.Equal("observer-near", nearResult!.ObserverId);
        Assert.Equal(3, nearResult.ObserverRevision);
        Assert.Equal("observer-distant", distantResult!.ObserverId);
        Assert.Equal(9, distantResult.ObserverRevision);
        Assert.Equal(12, nearResult.SourceRevision);
        Assert.Equal(12, distantResult.SourceRevision);
    }

    [Fact]
    public void ReplacingOnePanelFrameDoesNotMutateAnotherObserversPanel()
    {
        var router = new ObserverPanelFrameRouter();
        var firstObserver = new ObserverFrameEnvelope(
            "observer-a", 1, 5, 1, CreateFrame());
        var secondObserver = new ObserverFrameEnvelope(
            "observer-b", 1, 5, 1, CreateFrame());
        var revisedFirstObserver = new ObserverFrameEnvelope(
            "observer-a", 2, 5, 2, CreateFrame());

        router.Route(firstObserver, ["a-panel"]);
        router.Route(secondObserver, ["b-panel"]);
        router.Route(revisedFirstObserver, ["a-panel"]);

        Assert.True(router.TryGetFrame("a-panel", out var a));
        Assert.True(router.TryGetFrame("b-panel", out var b));
        Assert.Same(revisedFirstObserver, a);
        Assert.Same(secondObserver, b);
        Assert.Equal(1, b!.ObserverRevision);
    }

    private static RenderFrame CreateFrame() => new(
        new RenderView(Vector3.Zero, Matrix4x4.Identity, Matrix4x4.Identity),
        Array.Empty<RenderDrawCommand>());
}
