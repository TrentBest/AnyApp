using TheSingularityWorkshop.MicroBundleDomain;
using System.Net.Http;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleRepository.Core;
using TheSingularityWorkshop.MicroBundleRepository.Rest;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Preloads repository-backed MicroBundles before synchronous FSM_COS composition.
/// Repository delivery remains separate from materialization and arbitration.
/// </summary>
public sealed class RepositoryMicroBundleCatalog : IMicroBundleCatalog
{
    private readonly RestMicroBundleRepository _repository;
    private readonly IReadOnlyDictionary<ulong, MicroBundleArtifactAddress> _addresses;
    private readonly Dictionary<ulong, IMicroBundle> _loaded = new();

    public RepositoryMicroBundleCatalog(
        Uri repositoryEndpoint,
        ExperienceManifest manifest,
        HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(repositoryEndpoint);
        ArgumentNullException.ThrowIfNull(manifest);

        if (!manifest.UsesRepositoryArtifacts)
            throw new ArgumentException(
                "The Experience manifest must provide artifact identity for every MicroBundle request.",
                nameof(manifest));

        var addresses = new Dictionary<ulong, MicroBundleArtifactAddress>();
        foreach (var request in manifest.Bundles)
        {
            addresses[request.BundleId] = new MicroBundleArtifactAddress(
                request.BundleId,
                request.ArtifactVersion!,
                request.ContentHash!);
        }

        _addresses = addresses;
        var client = httpClient ?? new HttpClient();
        _repository = new RestMicroBundleRepository(
            repositoryEndpoint,
            new TheSingularityWorkshop.FSM_REST.HttpClientRestTransport(client));
    }

    public async Task PreloadClosureAsync(
        IEnumerable<ulong> rootBundleIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootBundleIds);

        var pending = new Queue<ulong>(rootBundleIds);
        var visited = new HashSet<ulong>();

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bundleId = pending.Dequeue();
            if (!visited.Add(bundleId) || _loaded.ContainsKey(bundleId))
                continue;

            if (!_addresses.TryGetValue(bundleId, out var address))
                throw new InvalidOperationException(
                    $"No repository address is registered for MicroBundle {bundleId}.");

            var artifact = await _repository.GetAsync(address, cancellationToken);
            if (artifact is null)
                throw new InvalidOperationException(
                    $"MicroBundle artifact {address} was not found in the repository.");

            var bundle = RepositoryAssemblyMicroBundleMaterializer.Materialize(artifact);
            ArgumentNullException.ThrowIfNull(bundle);

            if (bundle.Id != bundleId)
                throw new InvalidOperationException(
                    $"Materialized artifact {address} produced MicroBundle {bundle.Id}.");

            _loaded.Add(bundle.Id, bundle);

            foreach (var dependency in bundle.Dependencies)
                pending.Enqueue(dependency.BundleId);
        }
    }

    public bool TryResolve(ulong bundleId, out IMicroBundle? bundle) =>
        _loaded.TryGetValue(bundleId, out bundle);
}
