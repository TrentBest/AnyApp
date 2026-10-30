#if WORKSHOP_USE_REPOSITORY_SOURCES
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

/// <summary>
/// Exercises the real REST repository adapter and assembly materializer against a local fake HTTP endpoint.
/// </summary>
public sealed class RepositoryMicroBundleCatalogIntegrationTests
{
    private const ulong RootBundleId = 9401;
    private const ulong DependencyBundleId = 9402;
    private const string BundleVersion = "1.0.0";

    [Fact]
    public async Task PreloadClosureAsync_LoadsTransitiveArtifactWithoutPromotingItToRuntimeRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AnyAppRepositoryCatalogTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var assemblyBytes = await File.ReadAllBytesAsync(typeof(RootTestBundle).Assembly.Location);
            var assemblyHash = Hash(assemblyBytes);
            var emptyConfiguration = Array.Empty<byte>();
            var configurationHash = Hash(emptyConfiguration);
            using var client = new HttpClient(new ArtifactHandler(new Dictionary<string, byte[]>
            {
                [assemblyHash] = assemblyBytes,
                [configurationHash] = emptyConfiguration
            }));

            var manifest = new ExperienceManifest(
                9400,
                BundleVersion,
                9403,
                [
                    new ExperienceBundleRequest(
                        RootBundleId,
                        ArtifactVersion: BundleVersion,
                        ContentHash: assemblyHash,
                        BundleVersion: BundleVersion,
                        ConfigurationArtifactVersion: "configuration-1.0.0",
                        ConfigurationContentHash: configurationHash)
                ],
                ResolvedPlan: new ExperienceResolvedPlan(
                    1,
                    [DependencyBundleId, RootBundleId],
                    [new ExperienceResolvedDependency(RootBundleId, DependencyBundleId)]))
            {
                ArtifactCatalogue =
                [
                    new ExperienceArtifactIdentity(RootBundleId, BundleVersion, assemblyHash),
                    new ExperienceArtifactIdentity(DependencyBundleId, BundleVersion, assemblyHash)
                ],
                ResolvedConfigurationCatalogue =
                [
                    new ExperienceResolvedConfigurationIdentity(
                        RootBundleId, BundleVersion, "configuration-1.0.0", configurationHash),
                    new ExperienceResolvedConfigurationIdentity(
                        DependencyBundleId, BundleVersion, "configuration-1.0.0", configurationHash)
                ]
            };

            var runtimeManifest = manifest.ToRuntimeManifest();
            Assert.Single(runtimeManifest.Bundles);
            Assert.Equal(RootBundleId, runtimeManifest.Bundles[0].BundleId);
            Assert.Equal(
                new[] { DependencyBundleId, RootBundleId },
                runtimeManifest.LoadPlan!.Select(entry => entry.Reference.BundleId));

            var catalog = new RepositoryMicroBundleCatalog(
                new Uri("http://localhost/"),
                manifest,
                client,
                Path.Combine(tempRoot, "cache"));

            await catalog.PreloadClosureAsync(runtimeManifest.Bundles.Select(request => request.BundleId));

            Assert.True(catalog.TryResolve(RootBundleId, BundleVersion, out var root));
            Assert.NotNull(root);
            Assert.Equal(RootBundleId, root!.Descriptor.Id);
            Assert.True(catalog.TryResolve(DependencyBundleId, BundleVersion, out var dependency));
            Assert.NotNull(dependency);
            Assert.Equal(DependencyBundleId, dependency!.Descriptor.Id);

            var configurationSource = await catalog.CreateConfigurationSourceAsync(manifest);
            // FSM_COS must load every pinned plan entry while preserving Bundles as authored roots.
            var assembly = new FsmCos(catalog).Execute(runtimeManifest, configurationSource);

            Assert.Equal(2, assembly.Bundles.Count);
            Assert.Contains(assembly.Bundles, bundle => bundle.Id == RootBundleId);
            Assert.Contains(assembly.Bundles, bundle => bundle.Id == DependencyBundleId);
            Assert.Single(runtimeManifest.Bundles);
            Assert.DoesNotContain(runtimeManifest.Bundles, request => request.BundleId == DependencyBundleId);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    public sealed class RootTestBundle : IMicroBundle, IMicroBundleResolvedConfiguration
    {
        public MicroBundleDescriptor Descriptor { get; } =
            new(RootBundleId, BundleVersion, [new MicroBundleDependency(DependencyBundleId)]);

        public IReadOnlyList<MicroBundleDependencyRequest> Dependencies { get; } =
            [MicroBundleDependencyRequest.Unconfigured(DependencyBundleId)];

        public ReadOnlyMemory<byte> ExportResolvedConfiguration() => ReadOnlyMemory<byte>.Empty;

        public void Load(IMicroBundleLoadContext context) { }

        public bool Arbitrate(IMicroBundleArbitrationContext context, int roundIndex) => false;
    }

    public sealed class DependencyTestBundle : IMicroBundle, IMicroBundleResolvedConfiguration
    {
        public MicroBundleDescriptor Descriptor { get; } = new(DependencyBundleId, BundleVersion);

        public IReadOnlyList<MicroBundleDependencyRequest> Dependencies { get; } = [];

        public void Load(IMicroBundleLoadContext context) { }

        public bool Arbitrate(IMicroBundleArbitrationContext context, int roundIndex) => false;
    }

    private sealed class ArtifactHandler(IReadOnlyDictionary<string, byte[]> contentByHash) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segments = request.RequestUri!.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 5 ||
                segments[0] != "api" ||
                segments[1] != "microbundles" ||
                !ulong.TryParse(segments[2], out var bundleId))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var version = Uri.UnescapeDataString(segments[3]);
            var hash = segments[4];
            if (!contentByHash.TryGetValue(hash, out var content))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var dto = new
            {
                bundleId,
                version,
                contentHash = hash,
                contentBase64 = Convert.ToBase64String(content),
                semanticAddress = (object?)null
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(dto), System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
#endif
