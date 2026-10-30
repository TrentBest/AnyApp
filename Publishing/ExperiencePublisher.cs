#if WORKSHOP_USE_REPOSITORY_SOURCES
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.MicroBundleRepository.Core;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Resolves an authored Experience during publication, uploads immutable MicroBundle and
/// resolved-configuration artifacts, and writes a manifest that runtime can assemble without
/// repeating arbitration.
/// </summary>
public sealed class ExperiencePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IMicroBundleCatalog _catalog;
    private readonly IMicroBundleRepository _repository;

    public ExperiencePublisher(IMicroBundleCatalog catalog, IMicroBundleRepository repository)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(repository);
        _catalog = catalog;
        _repository = repository;
    }

    /// <summary>
    /// Publishes the resolved bundle/configuration artifacts and writes the immutable manifest last.
    /// The manifest path must not already exist; a failed publication may leave harmless immutable
    /// artifacts in the repository, but it never writes a runnable manifest before all uploads succeed.
    /// </summary>
    public async Task<ExperiencePublicationResult> PublishAsync(
        ExperienceManifest authoredManifest,
        string manifestOutputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authoredManifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestOutputPath);

        if (authoredManifest.ResolvedPlan is not null)
            throw new ArgumentException(
                "PublishAsync expects an authored manifest, not a previously resolved publication.",
                nameof(authoredManifest));

        var fullManifestPath = Path.GetFullPath(manifestOutputPath);
        if (File.Exists(fullManifestPath))
            throw new IOException(
                $"Refusing to overwrite immutable Experience publication '{fullManifestPath}'.");

        var outputDirectory = Path.GetDirectoryName(fullManifestPath)
            ?? throw new InvalidOperationException("The manifest output path has no parent directory.");
        Directory.CreateDirectory(outputDirectory);

        // Resolve the authored roots and run bounded arbitration exactly once, during publication.
        var runtimeAssembly = new FsmCos(_catalog).Execute(
            authoredManifest.ToRuntimeManifest(),
            authoredManifest.CreateConfigurationSource());

        if (runtimeAssembly.Bundles.Count == 0)
            throw new InvalidOperationException("Cannot publish an Experience with an empty MicroBundle closure.");

        if (runtimeAssembly.ResolvedConfigurations.Count != runtimeAssembly.Bundles.Count)
        {
            var missing = runtimeAssembly.Bundles
                .Where(bundle => !runtimeAssembly.ResolvedConfigurations.ContainsKey(bundle.Id))
                .Select(bundle => bundle.Id)
                .OrderBy(id => id);
            throw new InvalidOperationException(
                "Every MicroBundle must implement the resolved-configuration contract before this Experience can be published. " +
                $"Missing bundle IDs: [{string.Join(", ", missing)}].");
        }

        var bundleIds = runtimeAssembly.Bundles.Select(bundle => bundle.Id).ToHashSet();
        var authoredRootIds = authoredManifest.Bundles.Select(request => request.BundleId).ToArray();
        if (authoredRootIds.Distinct().Count() != authoredRootIds.Length)
            throw new ArgumentException(
                "An authored Experience cannot request the same MicroBundle composition root more than once.",
                nameof(authoredManifest));

        var rootIds = authoredRootIds.ToHashSet();
        var requestsByRootId = new Dictionary<ulong, ExperienceBundleRequest>();
        var artifactCatalogue = new List<ExperienceArtifactIdentity>(runtimeAssembly.Bundles.Count);
        var configurationCatalogue = new List<ExperienceResolvedConfigurationIdentity>(runtimeAssembly.Bundles.Count);
        var bundleArtifacts = new List<MicroBundleArtifact>(runtimeAssembly.Bundles.Count);
        var configurationArtifacts = new List<MicroBundleArtifact>(runtimeAssembly.Bundles.Count);
        var bundleAddresses = new List<MicroBundleArtifactAddress>(runtimeAssembly.Bundles.Count);
        var configurationAddresses = new List<MicroBundleArtifactAddress>(runtimeAssembly.Bundles.Count);
        var configurationDirectory = Path.Combine(outputDirectory, "configuration");
        Directory.CreateDirectory(configurationDirectory);

        var dependencies = new List<ExperienceResolvedDependency>();
        foreach (var bundle in runtimeAssembly.Bundles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dependencyRequests = bundle.Dependencies
                ?? throw new InvalidOperationException($"MicroBundle {bundle.Id} returned no dependency request list.");
            foreach (var dependency in dependencyRequests)
            {
                if (!bundleIds.Contains(dependency.BundleId))
                    throw new InvalidOperationException(
                        $"Resolved MicroBundle {bundle.Id} depends on {dependency.BundleId}, which is absent from the runtime assembly.");
                dependencies.Add(new ExperienceResolvedDependency(bundle.Id, dependency.BundleId));
            }

            var assemblyPath = bundle.GetType().Assembly.Location;
            if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                throw new InvalidOperationException(
                    $"Cannot publish MicroBundle {bundle.Id}: its assembly has no readable file location. " +
                    "Publication must run against build/source assemblies, not an opaque dynamically loaded artifact.");

            var assemblyBytes = await File.ReadAllBytesAsync(assemblyPath, cancellationToken);
            var bundleHash = Sha256(assemblyBytes);
            var bundleAddress = new MicroBundleArtifactAddress(
                bundle.Id,
                bundle.Descriptor.Version,
                bundleHash);
            bundleArtifacts.Add(new MicroBundleArtifact(bundleAddress, assemblyBytes));
            bundleAddresses.Add(bundleAddress);
            artifactCatalogue.Add(new ExperienceArtifactIdentity(
                bundle.Id, bundleAddress.Version, bundleAddress.ContentHash));

            if (!runtimeAssembly.TryGetResolvedConfiguration(bundle.Id, out var resolvedConfiguration))
                throw new InvalidOperationException(
                    $"FSM_COS did not return resolved configuration for MicroBundle {bundle.Id}.");

            var configurationBytes = resolvedConfiguration.ToArray();
            var configurationVersion = $"configuration-{bundle.Descriptor.Version}";
            var configurationHash = Sha256(configurationBytes);
            var configurationAddress = new MicroBundleArtifactAddress(
                bundle.Id,
                configurationVersion,
                configurationHash);
            configurationArtifacts.Add(new MicroBundleArtifact(configurationAddress, configurationBytes));
            configurationAddresses.Add(configurationAddress);

            var configurationRelativePath = $"configuration/{bundle.Id}.bin";
            configurationCatalogue.Add(new ExperienceResolvedConfigurationIdentity(
                bundle.Id, bundle.Descriptor.Version, configurationVersion, configurationHash, configurationRelativePath));
            var configurationPath = Path.Combine(configurationDirectory, $"{bundle.Id}.bin");
            if (File.Exists(configurationPath))
                throw new IOException(
                    $"Refusing to overwrite immutable resolved configuration '{configurationPath}'.");
            await File.WriteAllBytesAsync(configurationPath, configurationBytes, cancellationToken);

            if (rootIds.Contains(bundle.Id))
            {
                requestsByRootId.Add(bundle.Id, new ExperienceBundleRequest(
                    bundle.Id,
                    ConfigurationBase64: "",
                    ArtifactVersion: bundle.Descriptor.Version,
                    ContentHash: bundleHash,
                    BundleVersion: bundle.Descriptor.Version,
                    ConfigurationPath: configurationRelativePath,
                    ConfigurationArtifactVersion: configurationVersion,
                    ConfigurationContentHash: configurationHash));
            }
        }

        var missingRoots = rootIds.Where(rootId => !requestsByRootId.ContainsKey(rootId)).OrderBy(id => id).ToArray();
        if (missingRoots.Length > 0)
            throw new InvalidOperationException(
                $"Published runtime assembly omitted requested composition roots: [{string.Join(", ", missingRoots)}].");

        var requests = authoredManifest.Bundles
            .Select(root => requestsByRootId[root.BundleId])
            .ToArray();
        var resolvedPlan = new ExperienceResolvedPlan(
            runtimeAssembly.ArbitrationRounds,
            runtimeAssembly.Bundles.Select(bundle => bundle.Id).ToArray(),
            dependencies.Distinct().ToArray());
        var publishedManifest = authoredManifest with
        {
            Bundles = requests,
            ArtifactCatalogue = artifactCatalogue,
            ResolvedConfigurationCatalogue = configurationCatalogue,
            ResolvedPlan = resolvedPlan,
            ConfigurationRoot = outputDirectory
        };

        // Upload all immutable inputs before making the manifest discoverable.
        foreach (var artifact in bundleArtifacts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.PutAsync(artifact, cancellationToken);
        }

        foreach (var artifact in configurationArtifacts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _repository.PutAsync(artifact, cancellationToken);
        }

        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(publishedManifest, JsonOptions);
        await File.WriteAllBytesAsync(fullManifestPath, manifestBytes, cancellationToken);

        return new ExperiencePublicationResult(
            publishedManifest,
            fullManifestPath,
            Sha256(manifestBytes),
            bundleAddresses,
            configurationAddresses);
    }

    private static string Sha256(ReadOnlySpan<byte> content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}

/// <summary>Output and immutable artifact identities from an Experience publication.</summary>
public sealed record ExperiencePublicationResult(
    ExperienceManifest Manifest,
    string ManifestPath,
    string ManifestContentHash,
    IReadOnlyList<MicroBundleArtifactAddress> BundleArtifacts,
    IReadOnlyList<MicroBundleArtifactAddress> ConfigurationArtifacts);
#endif
