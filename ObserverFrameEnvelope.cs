using TheSingularityWorkshop.Renderer;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Host-owned provenance envelope associating one observer evaluation with its immutable Renderer frame.
/// Observer and panel identity stay outside Renderer and GUI public contracts.
/// </summary>
public sealed record ObserverFrameEnvelope
{
    public ObserverFrameEnvelope(
        string observerId,
        long observerRevision,
        long sourceRevision,
        long frameSequence,
        RenderFrame frame)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(observerId);
        ArgumentOutOfRangeException.ThrowIfNegative(observerRevision);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceRevision);
        ArgumentOutOfRangeException.ThrowIfNegative(frameSequence);
        ArgumentNullException.ThrowIfNull(frame);

        ObserverId = observerId;
        ObserverRevision = observerRevision;
        SourceRevision = sourceRevision;
        FrameSequence = frameSequence;
        Frame = frame;
    }

    public string ObserverId { get; }
    public long ObserverRevision { get; }
    public long SourceRevision { get; }
    public long FrameSequence { get; }
    public RenderFrame Frame { get; }
}

/// <summary>
/// Routes a completed observer frame to one or more host presentation panels.
/// Routing is fan-out only: it never evaluates observer policy or creates another Renderer frame.
/// </summary>
public sealed class ObserverPanelFrameRouter
{
    private readonly Dictionary<string, ObserverFrameEnvelope> _frames =
        new(StringComparer.Ordinal);

    public void Route(ObserverFrameEnvelope envelope, IEnumerable<string> panelIds)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(panelIds);

        var targets = panelIds.ToArray();
        if (targets.Length == 0)
            throw new ArgumentException("At least one panel is required.", nameof(panelIds));

        foreach (var panelId in targets)
            ArgumentException.ThrowIfNullOrWhiteSpace(panelId);

        foreach (var panelId in targets)
            _frames[panelId] = envelope;
    }

    public bool TryGetFrame(string panelId, out ObserverFrameEnvelope? envelope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panelId);
        return _frames.TryGetValue(panelId, out envelope);
    }
}
