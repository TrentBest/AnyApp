using System.Net.Http;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Host-neutral policy for resuming the last Experience against the current publication catalog.
/// The host supplies storage and catalog operations; this type never opens UI or performs network I/O itself.
/// </summary>
public static class ExperienceStartupCoordinator
{
    /// <summary>Reconciles a cached Experience with the publisher's current immutable identity.</summary>
    public static async Task<ExperienceStartupDecision> ReconcileAsync(
        Func<CancellationToken, Task<ExperienceManifest?>> loadManifestAsync,
        Func<CancellationToken, Task<PublishedExperience?>> loadPublicationAsync,
        Func<CancellationToken, Task<IReadOnlyList<PublishedExperience>>> listPublicationsAsync,
        Func<PublishedExperience, CancellationToken, Task<ExperienceManifest>> fetchManifestAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loadManifestAsync);
        ArgumentNullException.ThrowIfNull(loadPublicationAsync);
        ArgumentNullException.ThrowIfNull(listPublicationsAsync);
        ArgumentNullException.ThrowIfNull(fetchManifestAsync);

        ExperienceManifest? cachedManifest;
        PublishedExperience? cachedPublication;
        try
        {
            cachedManifest = await loadManifestAsync(cancellationToken).ConfigureAwait(false);
            cachedPublication = await loadPublicationAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return ExperienceStartupDecision.LaunchForge;
        }

        if (cachedManifest is null)
            return ExperienceStartupDecision.LaunchForge;

        IReadOnlyList<PublishedExperience> publications;
        try
        {
            publications = await listPublicationsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Network unavailability is not evidence of freshness. Resume locally without
            // persisting a new publication identity; the next launch must reconcile again.
            return new ExperienceStartupDecision(cachedManifest, null, PersistLastManifest: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // HttpClient timeouts also use OperationCanceledException. When the caller's
            // token was not canceled, treat this as unavailable catalog access, not user intent.
            return new ExperienceStartupDecision(cachedManifest, null, PersistLastManifest: false);
        }
        catch
        {
            return ExperienceStartupDecision.LaunchForge;
        }

        var current = publications.FirstOrDefault(item => item.ExperienceId == cachedManifest.ExperienceId);
        if (current is null)
        {
            return cachedPublication is not null
                ? ExperienceStartupDecision.LaunchForge
                : new ExperienceStartupDecision(cachedManifest, null, PersistLastManifest: true);
        }

        if (cachedPublication is not null &&
            IsSamePublication(current, cachedPublication) &&
            string.Equals(cachedManifest.Version, current.Version, StringComparison.Ordinal))
        {
            return new ExperienceStartupDecision(cachedManifest, current, PersistLastManifest: true);
        }

        try
        {
            var refreshed = await fetchManifestAsync(current, cancellationToken).ConfigureAwait(false);
            if (refreshed.ExperienceId != current.ExperienceId ||
                !string.Equals(refreshed.Version, current.Version, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The current published Experience manifest has a different identity.");
            }

            return new ExperienceStartupDecision(refreshed, current, PersistLastManifest: true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return ExperienceStartupDecision.LaunchForge;
        }
    }

    private static bool IsSamePublication(PublishedExperience left, PublishedExperience right) =>
        left.ExperienceId == right.ExperienceId &&
        string.Equals(left.Version, right.Version, StringComparison.Ordinal) &&
        string.Equals(left.ContentHash, right.ContentHash, StringComparison.OrdinalIgnoreCase);
}

/// <summary>The host action selected by publication reconciliation.</summary>
public sealed record ExperienceStartupDecision(
    ExperienceManifest? Manifest,
    PublishedExperience? Publication,
    bool PersistLastManifest)
{
    public static ExperienceStartupDecision LaunchForge { get; } = new(null, null, false);
    public bool ShouldLaunchForge => Manifest is null;
}
