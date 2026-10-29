#if WORKSHOP_USE_REPOSITORY_SOURCES
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.MicroBundleRepository.Core;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class ExperiencePublisherTests
{
    [Fact]
    public async Task PublishAsync_WritesFlatResolvedManifestAndPublishesSeparateConfigurationArtifact()
    {
        var root = Path.Combine(Path.GetTempPath(), "AnyAppPublisherTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var bundle = new PublishableBundle(9001);
            var repository = new MemoryRepository();
            var publisher = new ExperiencePublisher(new SingleBundleCatalog(bundle), repository);
            var authored = new ExperienceManifest(
                9000,
                "1.0.0",
                9002,
                [new ExperienceBundleRequest(9001)]);

            var result = await publisher.PublishAsync(authored, Path.Combine(root, "manifest.json"));

            Assert.True(File.Exists(result.ManifestPath));
            Assert.True(File.Exists(Path.Combine(root, "configuration", "9001.bin")));
            Assert.Single(result.BundleArtifacts);
            Assert.Single(result.ConfigurationArtifacts);
            Assert.Equal(2, repository.Count);
            Assert.NotNull(result.Manifest.ResolvedPlan);
            Assert.Equal(new ulong[] { 9001 }, result.Manifest.ResolvedPlan!.LoadOrder);
            Assert.True(result.Manifest.UsesRepositoryArtifacts);
            Assert.True(result.Manifest.UsesResolvedRepositoryConfigurations);
            Assert.Single(result.Manifest.Bundles);
            Assert.Single(result.Manifest.ArtifactCatalogue!);

            var loaded = ExperienceManifest.LoadFile(result.ManifestPath);
            var runtimeManifest = loaded.ToRuntimeManifest();
            Assert.True(runtimeManifest.ArbitrationResolved);
            Assert.Equal(new ulong[] { 9001 }, runtimeManifest.LoadPlan!.Select(entry => entry.Reference.BundleId));

            var source = loaded.CreateConfigurationSource();
            Assert.True(source.TryGetConfiguration(9002, 9001, "1.0.0", out var configuration));
            Assert.Empty(configuration.ToArray());

            var arbitrationCallsAfterPublish = bundle.ArbitrationCalls;
            var runtimeAssembly = new FsmCos(new SingleBundleCatalog(bundle)).Execute(runtimeManifest, source);
            Assert.Equal(arbitrationCallsAfterPublish, bundle.ArbitrationCalls);
            Assert.Single(runtimeAssembly.Bundles);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ToRuntimeManifest_KeepsDependencyClosureOutOfCompositionRoots()
    {
        var manifest = new ExperienceManifest(
            9100,
            "1.0.0",
            9101,
            [new ExperienceBundleRequest(
                9102,
                ArtifactVersion: "1.0.0",
                ContentHash: new string('a', 64),
                BundleVersion: "1.0.0",
                ConfigurationArtifactVersion: "configuration-1.0.0",
                ConfigurationContentHash: new string('b', 64))],
            ResolvedPlan: new ExperienceResolvedPlan(
                0,
                [9102, 9103, 9104],
                [
                    new ExperienceResolvedDependency(9102, 9103),
                    new ExperienceResolvedDependency(9103, 9104)
                ]))
        {
            ArtifactCatalogue =
            [
                new ExperienceArtifactIdentity(9102, "1.0.0", new string('a', 64)),
                new ExperienceArtifactIdentity(9103, "1.0.0", new string('c', 64)),
                new ExperienceArtifactIdentity(9104, "2.0.0", new string('d', 64))
            ]
        };

        var runtimeManifest = manifest.ToRuntimeManifest();

        Assert.Equal(new ulong[] { 9102 }, runtimeManifest.Bundles.Select(request => request.BundleId));
        Assert.Equal(
            new ulong[] { 9102, 9103, 9104 },
            runtimeManifest.LoadPlan!.Select(entry => entry.Reference.BundleId));
        Assert.Equal(
            new[] { "1.0.0", "1.0.0", "2.0.0" },
            runtimeManifest.LoadPlan!.Select(entry => entry.Reference.Version));
    }

    [Fact]
    public void ToRuntimeManifest_RejectsMissingTransitiveArtifactIdentityBeforeComposition()
    {
        var manifest = new ExperienceManifest(
            9150,
            "1.0.0",
            9151,
            [
                new ExperienceBundleRequest(
                    9152,
                    ArtifactVersion: "1.0.0",
                    ContentHash: new string('a', 64),
                    BundleVersion: "1.0.0",
                    ConfigurationArtifactVersion: "configuration-1.0.0",
                    ConfigurationContentHash: new string('b', 64))
            ],
            ResolvedPlan: new ExperienceResolvedPlan(
                0,
                [9152, 9153],
                [new ExperienceResolvedDependency(9152, 9153)]))
        {
            ArtifactCatalogue =
            [
                new ExperienceArtifactIdentity(9152, "1.0.0", new string('a', 64))
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => manifest.ToRuntimeManifest());

        Assert.Contains("MicroBundle 9153", exception.Message);
        Assert.Contains("absent from the Experience artifact catalogue", exception.Message);
    }

    [Fact]
    public void RepositoryManifest_KeepsConfigurationOnRootRequestNotDependencyCatalogue()
    {
        var rootConfigurationHash = new string('b', 64);
        var manifest = new ExperienceManifest(
            9160,
            "1.0.0",
            9161,
            [
                new ExperienceBundleRequest(
                    9162,
                    ArtifactVersion: "1.0.0",
                    ContentHash: new string('a', 64),
                    BundleVersion: "1.0.0",
                    ConfigurationArtifactVersion: "configuration-1.0.0",
                    ConfigurationContentHash: rootConfigurationHash)
            ],
            ResolvedPlan: new ExperienceResolvedPlan(
                0,
                [9162, 9163],
                [new ExperienceResolvedDependency(9162, 9163)]))
        {
            ArtifactCatalogue =
            [
                new ExperienceArtifactIdentity(9162, "1.0.0", new string('a', 64)),
                new ExperienceArtifactIdentity(9163, "2.0.0", new string('c', 64))
            ]
        };

        var runtimeManifest = manifest.ToRuntimeManifest();

        var root = Assert.Single(runtimeManifest.Bundles);
        Assert.Equal(9162UL, root.BundleId);
        Assert.Equal("configuration-1.0.0", manifest.Bundles[0].ConfigurationArtifactVersion);
        Assert.Equal(rootConfigurationHash, manifest.Bundles[0].ConfigurationContentHash);
        Assert.DoesNotContain(manifest.Bundles, request => request.BundleId == 9163);
        Assert.Contains(manifest.GetArtifactCatalogue(), identity => identity.BundleId == 9163);
    }

    [Fact]
    public void RepositoryCatalog_RejectsConflictingArtifactIdentities()
    {
        var manifest = new ExperienceManifest(
            9200,
            "1.0.0",
            9201,
            [new ExperienceBundleRequest(
                9202,
                ArtifactVersion: "1.0.0",
                ContentHash: new string('a', 64),
                BundleVersion: "1.0.0")])
        {
            ArtifactCatalogue =
            [
                new ExperienceArtifactIdentity(9202, "1.0.0", new string('a', 64)),
                new ExperienceArtifactIdentity(9202, "2.0.0", new string('b', 64))
            ]
        };

        var exception = Assert.Throws<ArgumentException>(() =>
            new RepositoryMicroBundleCatalog(new Uri("http://localhost/"), manifest));

        Assert.Contains("conflicting immutable identities", exception.Message);
    }

    private sealed class SingleBundleCatalog(IMicroBundle bundle) : IMicroBundleCatalog
    {
        public bool TryResolve(ulong bundleId, out IMicroBundle? resolved)
        {
            resolved = bundle.Id == bundleId ? bundle : null;
            return resolved is not null;
        }

        public bool TryResolve(ulong bundleId, string version, out IMicroBundle? resolved)
        {
            resolved = bundle.Id == bundleId && bundle.Descriptor.Version == version ? bundle : null;
            return resolved is not null;
        }
    }

    private sealed class MemoryRepository : IMicroBundleRepository
    {
        private readonly Dictionary<MicroBundleArtifactAddress, MicroBundleArtifact> _artifacts = new();

        public int Count => _artifacts.Count;

        public ValueTask<MicroBundleArtifact?> GetAsync(
            MicroBundleArtifactAddress address,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _artifacts.TryGetValue(address, out var artifact);
            return ValueTask.FromResult(artifact);
        }

        public ValueTask PutAsync(
            MicroBundleArtifact artifact,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _artifacts[artifact.Address] = artifact;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PublishableBundle(ulong id) : IMicroBundle, IMicroBundleResolvedConfiguration
    {
        private ReadOnlyMemory<byte> _configuration;

        public MicroBundleDescriptor Descriptor { get; } = new(id, "1.0.0");
        public IReadOnlyList<MicroBundleDependencyRequest> Dependencies { get; } = Array.Empty<MicroBundleDependencyRequest>();
        public int ArbitrationCalls { get; private set; }

        public void Load(IMicroBundleLoadContext context)
        {
            _configuration = context.TryGetConfiguration(id, out var configuration)
                ? configuration.ToArray()
                : ReadOnlyMemory<byte>.Empty;
        }

        public bool Arbitrate(IMicroBundleArbitrationContext context, int roundIndex)
        {
            ArbitrationCalls++;
            return false;
        }

        public ReadOnlyMemory<byte> ExportResolvedConfiguration() => _configuration.ToArray();
    }
}
#endif
