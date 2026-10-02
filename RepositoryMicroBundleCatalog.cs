using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.FSM_REST;
using TheSingularityWorkshop.MicroBundleRepository.Core;
using TheSingularityWorkshop.MicroBundleRepository.Rest;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Repository-backed MicroBundle catalog used by AnyApp when an Experience
/// manifest supplies immutable artifact identities.
/// </summary>
public sealed class RepositoryMicroBundleCatalog
{
    private readonly RestMicroBundleRepository _repository;
    private readonly IReadOnlyDictionary<ulong, MicroBundleArtifactAddress> _addresses;
    private readonly IMicroBundleArtifactMaterializer _materializer;

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
        _materializer = new AssemblyMicroBundleArtifactMaterializer();

        var client = httpClient ?? new HttpClient();
        _repository = new RestMicroBundleRepository(
            repositoryEndpoint,
            new HttpClientRestTransport(client));
    }

    public async Task<RuntimeAssembly> ComposeAsync(
        RuntimeManifest manifest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var catalog = new RestMicroBundleCatalog(
            _repository,
            _addresses,
            _materializer);

        await catalog.PreloadClosureAsync(
            manifest.Bundles.Select(bundle => bundle.BundleId),
            cancellationToken);

        return new FsmCos(catalog).Execute(manifest);
    }
}
