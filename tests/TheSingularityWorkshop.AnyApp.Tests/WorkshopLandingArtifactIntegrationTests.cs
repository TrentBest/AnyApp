#if WORKSHOP_THE_SOURCE
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.GUI.WPF;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.MicroBundleRepository.Core;
using TheSingularityWorkshop.Workshop.Gui;
using TheSingularityWorkshop.Workshop.MicroBundle;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

/// <summary>
/// Proves that The's real Workshop Landing MicroBundle artifact crosses the repository,
/// materialization, FSM_COS, semantic GUI, and native WPF boundaries without a host special case.
/// </summary>
public sealed class WorkshopLandingArtifactIntegrationTests
{
    private const ulong BundleId = WorkshopLandingMicroBundle.BundleId;
    private const string BundleVersion = "1.0.0";
    private const string ArtifactVersion = "1.0.0";
    private const string ConfigurationArtifactVersion = "configuration-1.0.0";

    [Fact]
    public async Task PublishedWorkshopLandingArtifact_ComposesAndManifestsThroughWpf()
    {
        var assemblyBytes = await File.ReadAllBytesAsync(typeof(WorkshopLandingMicroBundle).Assembly.Location);
        var envelope = CreateEnvelope(BundleId, assemblyBytes);
        var artifactHash = Hash(envelope);
        var configurationBytes = Array.Empty<byte>();
        var configurationHash = Hash(configurationBytes);

        using var client = new HttpClient(new ArtifactHandler(new Dictionary<string, byte[]>
        {
            [artifactHash] = envelope,
            [configurationHash] = configurationBytes
        }));

        var manifest = new ExperienceManifest(
            ExperienceId: 2112,
            Version: "1.0.0",
            RuntimeId: 2113,
            Bundles:
            [
                new ExperienceBundleRequest(
                    BundleId,
                    ArtifactVersion: ArtifactVersion,
                    ContentHash: artifactHash,
                    BundleVersion: BundleVersion,
                    ConfigurationArtifactVersion: ConfigurationArtifactVersion,
                    ConfigurationContentHash: configurationHash)
            ],
            ResolvedPlan: new ExperienceResolvedPlan(
                ArbitrationRounds: 0,
                LoadOrder: [BundleId],
                Dependencies: []))
        {
            ArtifactCatalogue = [new ExperienceArtifactIdentity(BundleId, ArtifactVersion, artifactHash)],
            ResolvedConfigurationCatalogue =
            [
                new ExperienceResolvedConfigurationIdentity(
                    BundleId, BundleVersion, ConfigurationArtifactVersion, configurationHash)
            ]
        };

        var runtimeManifest = manifest.ToRuntimeManifest();
        Assert.Single(runtimeManifest.Bundles);
        Assert.Equal(BundleId, runtimeManifest.Bundles[0].BundleId);
        Assert.True(runtimeManifest.Bundles[0].IsPinned);

        var cacheDirectory = Path.Combine(
            Path.GetTempPath(), "AnyAppWorkshopLandingTests", Guid.NewGuid().ToString("N"));
        try
        {
            var catalog = new RepositoryMicroBundleCatalog(
                new Uri("http://localhost/"),
                manifest,
                client,
                cacheDirectory);

            await catalog.PreloadClosureAsync(runtimeManifest.Bundles.Select(request => request.BundleId));
            var configurationSource = await catalog.CreateConfigurationSourceAsync(manifest);

            Assert.True(catalog.TryResolve(BundleId, BundleVersion, out var materialized));
            Assert.NotNull(materialized);
            Assert.Equal(BundleId, materialized!.Id);
            Assert.IsType<WorkshopLandingMicroBundle>(materialized);

            var runtimeAssembly = new FsmCos(catalog).Execute(runtimeManifest, configurationSource);
            var composed = Assert.Single(runtimeAssembly.Bundles);
            Assert.Equal(BundleId, composed.Id);
            Assert.True(MicroBundleGuiSurface.TryGetRoot(composed, out var semanticRoot));
            Assert.NotNull(semanticRoot);
            Assert.Equal("workshop-landing-root", semanticRoot!.Id);
            Assert.Equal(GuiKinds.Panel, semanticRoot.Kind);

            // Exercise the actual GUI.WPF adapter on an STA thread, without opening a window
            // or confusing a successful native tree conversion with a visual desktop demo.
            FrameworkElement? nativeRoot = null;
            Exception? renderError = null;
            var uiThread = new Thread(() =>
            {
                try
                {
                    nativeRoot = WpfGuiRenderer.Render(semanticRoot);
                }
                catch (Exception exception)
                {
                    renderError = exception;
                }
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();

            Assert.Null(renderError);
            Assert.IsType<Border>(nativeRoot);
            Assert.Equal("workshop-landing-root", WpfGuiIdentity.GetNodeId(nativeRoot!));
        }
        finally
        {
            if (Directory.Exists(cacheDirectory))
                Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    private static byte[] CreateEnvelope(ulong bundleId, byte[] assemblyBytes)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(new byte[] { 0x46, 0x53, 0x4D, 0x42 }); // FSMB
            writer.Write((uint)1);
            writer.Write(bundleId);
            writer.Write((uint)assemblyBytes.Length);
            writer.Write(assemblyBytes);
            writer.Flush();
        }

        return stream.ToArray();
    }

    private static string Hash(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

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
                Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}
#endif
