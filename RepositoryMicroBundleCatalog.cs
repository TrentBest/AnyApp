using System.Security.Cryptography;
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

    public async Task<IMicroBundleConfigurationSource> CreateConfigurationSourceAsync(
        ExperienceManifest manifest,
        IMicroBundleConfigurationSource? localOverrides = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var values = new Dictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>>();

        foreach (var request in manifest.Bundles)
        {
            if (!request.HasConfigurationArtifactIdentity)
                continue;

            var address = new MicroBundleArtifactAddress(
                request.BundleId,
                request.ConfigurationArtifactVersion!,
                request.ConfigurationContentHash!);
            var artifact = await _repository.GetAsync(address, cancellationToken);
            var version = string.IsNullOrWhiteSpace(request.BundleVersion)
                ? string.IsNullOrWhiteSpace(request.ArtifactVersion) ? "1.0.0" : request.ArtifactVersion
                : request.BundleVersion;

            byte[] bytes;
            if (artifact is not null)
            {
                bytes = artifact.Content.ToArray();
            }
            else if (localOverrides is not null &&
                     localOverrides.TryGetConfiguration(manifest.RuntimeId, request.BundleId, version!, out var localConfiguration))
            {
                // Local sidecars/overrides are accepted as a cache only when their bytes
                // match the immutable identity recorded in the manifest.
                bytes = localConfiguration.ToArray();
                var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
                if (!string.Equals(actualHash, request.ConfigurationContentHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"Local resolved configuration for MicroBundle {request.BundleId} does not match its published SHA-256 identity.");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Resolved configuration artifact {address} for MicroBundle {request.BundleId} was not found.");
            }

            if (!values.TryAdd((request.BundleId, version!), bytes))
                throw new InvalidOperationException(
                    $"Experience manifest declares resolved configuration more than once for MicroBundle {request.BundleId}.");
        }

        return new RepositoryConfigurationSource(manifest.RuntimeId, values, localOverrides);
    }

    private sealed class RepositoryConfigurationSource(
        ulong runtimeId,
        IReadOnlyDictionary<(ulong BundleId, string Version), ReadOnlyMemory<byte>> values,
        IMicroBundleConfigurationSource? fallback) : IMicroBundleConfigurationSource
    {
        public bool TryGetConfiguration(
            ulong requestedRuntimeId,
            ulong bundleId,
            string version,
            out ReadOnlyMemory<byte> configuration)
        {
            if (requestedRuntimeId == runtimeId &&
                values.TryGetValue((bundleId, version), out configuration))
                return true;

            if (fallback is not null &&
                fallback.TryGetConfiguration(requestedRuntimeId, bundleId, version, out configuration))
                return true;

            configuration = default;
            return false;
        }
    }

    public bool TryResolve(ulong bundleId, out IMicroBundle? bundle) =>
        _loaded.TryGetValue(bundleId, out bundle);

    public bool TryResolve(ulong bundleId, string version, out IMicroBundle? bundle)
    {
        if (!_loaded.TryGetValue(bundleId, out bundle) || bundle is null)
            return false;

        if (string.IsNullOrWhiteSpace(version) ||
            string.Equals(bundle.Descriptor.Version, version, StringComparison.Ordinal))
            return true;

        bundle = null;
        return false;
    }
}
