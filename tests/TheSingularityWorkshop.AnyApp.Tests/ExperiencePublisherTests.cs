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
            Assert.Equal(1, result.BundleArtifacts.Count);
            Assert.Equal(1, result.ConfigurationArtifacts.Count);
            Assert.Equal(2, repository.Count);
            Assert.NotNull(result.Manifest.ResolvedPlan);
            Assert.Equal(new ulong[] { 9001 }, result.Manifest.ResolvedPlan!.LoadOrder);
            Assert.True(result.Manifest.UsesRepositoryArtifacts);
            Assert.True(result.Manifest.UsesResolvedRepositoryConfigurations);

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
