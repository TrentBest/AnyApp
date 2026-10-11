using System.Text;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.Experiences.Moniker;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.Renderer;
using TheSingularityWorkshop.MicroBundleDomain;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppRuntimeTests
{
    [Fact]
    public void Catalog_ResolvesBundlesByRequestedVersion()
    {
        var catalog = new AnyAppMicroBundleCatalog();

        Assert.True(catalog.TryResolve(
            MonikerWaveMotionMicroBundle.BundleId,
            "1.0.0",
            out var motion));
        Assert.NotNull(motion);

        Assert.False(catalog.TryResolve(
            MonikerWaveMotionMicroBundle.BundleId,
            "9.9.9",
            out _));
    }

    [Fact]
    public void Compose_ProducesMonikerExperienceThroughCos()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.Equal(MonikerExperience.RuntimeId, runtime.RuntimeId);
        Assert.Equal(3, runtime.Bundles.Count);
        Assert.Equal(0, runtime.ArbitrationRounds);
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(MonikerMicroBundle.BundleId, out var bundle));
        Assert.NotNull(bundle);
        Assert.True(runtime.TryGetBundle<MonikerWaveMotionMicroBundle>(
            MonikerWaveMotionMicroBundle.BundleId,
            out var motion));
        Assert.NotNull(motion);
        Assert.True(runtime.TryGetBundle<RendererMicroBundle>(
            RendererMicroBundle.BundleId,
            out var renderer));
        Assert.NotNull(renderer);
        Assert.True(renderer.IsLoaded);
    }

    [Fact]
    public void Compose_ProducesForgeEntryExperienceThroughCos()
    {
        var manifest = new ExperienceManifest(
            ForgeMicroBundle.ExperienceId,
            "1.0.0",
            ForgeMicroBundle.RuntimeId,
            [new ExperienceBundleRequest(ForgeMicroBundle.BundleId, "")]);

        var runtime = AnyAppRuntime.Compose(manifest.ToRuntimeManifest());

        Assert.Equal(ForgeMicroBundle.RuntimeId, runtime.RuntimeId);
        Assert.True(runtime.TryGetBundle<ForgeMicroBundle>(
            ForgeMicroBundle.BundleId,
            out var bundle));
        var surface = Assert.IsAssignableFrom<IAnyAppSurface>(bundle);
        Assert.Equal("forge-root", surface.Root.Id);
        Assert.True(surface.Root.TryFind("forge-explore", out var portal));
        Assert.Equal("Button", portal!.Kind);
    }

    [Fact]
    public void ComposedBundle_ExposesSemanticGuiSurface()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(MonikerMicroBundle.BundleId, out var bundle));
        Assert.True(MicroBundleGuiSurface.TryGetRoot(bundle!, out var root));
        Assert.NotNull(root);
        Assert.Equal("moniker-root", root!.Id);
        Assert.Equal("Panel", root.Kind);
        Assert.True(root.TryFind("moniker-singularity", out var word));
        Assert.Equal("Row", word!.Kind);
        Assert.Contains(word.Children, child => child.Text == "S" && !child.Properties.ContainsKey("wavePeriod"));
    }

    [Fact]
    public void ExperienceExecution_AddsWavePresentationWithoutMutatingBundle()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(MonikerMicroBundle.BundleId, out var bundle));
        Assert.True(MicroBundleGuiSurface.TryGetRoot(bundle!, out var root));
        Assert.NotNull(root);
        Assert.True(runtime.TryGetBundle<MonikerWaveMotionMicroBundle>(
            MonikerWaveMotionMicroBundle.BundleId,
            out var motion));
        Assert.NotNull(motion);
        var presented = MonikerExperience.ExecutePresentation(root!, motion);
        Assert.True(presented.TryFind("moniker-singularity-0", out var glyph));
        Assert.Equal("S", glyph!.Text);
        Assert.Equal("3.8", glyph.Properties["wavePeriod"]);
        Assert.Equal("7", glyph.Properties["waveAmplitude"]);
        Assert.False(root!.Find("moniker-singularity-0").Properties.ContainsKey("wavePeriod"));
    }

    [Fact]
    public void RendererInteractiveRepresentation_EnablesTheComposedMonikerMotion()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var bundle));
        Assert.True(MicroBundleGuiSurface.TryGetRoot(bundle!, out var root));
        Assert.True(runtime.TryGetBundle<MonikerWaveMotionMicroBundle>(
            MonikerWaveMotionMicroBundle.BundleId,
            out var motion));
        Assert.True(runtime.TryGetBundle<RendererMicroBundle>(
            RendererMicroBundle.BundleId,
            out var renderer));
        Assert.True(renderer!.IsLoaded);

        using var machine = new RendererComputationMachine(
            new RendererComputationContext("Test-Moniker", RepresentationTarget.Interactive));
        var representation = machine.Advance();

        var presented = MonikerExperience.ExecutePresentation(
            root!,
            motion,
            allowMotion: representation is RepresentationTarget.Near or RepresentationTarget.Interactive);

        Assert.True(presented.TryFind("moniker-singularity-0", out var glyph));
        Assert.Equal("3.8", glyph!.Properties["wavePeriod"]);
    }

    [Fact]
    public void ExperienceExecution_CanSuppressMotionOutsideNearAndInteractiveRepresentations()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var bundle));
        Assert.True(MicroBundleGuiSurface.TryGetRoot(bundle!, out var root));
        Assert.NotNull(root);
        Assert.True(runtime.TryGetBundle<MonikerWaveMotionMicroBundle>(
            MonikerWaveMotionMicroBundle.BundleId,
            out var motion));

        var presented = MonikerExperience.ExecutePresentation(root!, motion, allowMotion: false);

        Assert.Same(root, presented);
        Assert.True(presented.TryFind("moniker-singularity-0", out var glyph));
        Assert.False(glyph!.Properties.ContainsKey("wavePeriod"));
    }

    [Fact]
    public void EmptyManifest_ComposesWithoutThrowing()
    {
        var runtime = AnyAppRuntime.Compose(RuntimeManifest.Empty(999));
        Assert.Equal(999UL, runtime.RuntimeId);
        Assert.Empty(runtime.Bundles);
        Assert.Equal(0, runtime.ArbitrationRounds);
    }

    [Fact]
    public void ArtifactIdentity_IsDetectedWithoutChangingLegacyManifests()
    {
        var legacy = new ExperienceBundleRequest(3101, "");
        var published = new ExperienceBundleRequest(
            3101,
            "",
            "1.0.0",
            new string('a', 64));

        Assert.False(new ExperienceManifest(3101, "1.0.0", 3111, [legacy]).UsesRepositoryArtifacts);
        Assert.True(new ExperienceManifest(3101, "1.0.0", 3111, [published]).UsesRepositoryArtifacts);
    }

    [Fact]
    public void ManifestConfiguration_IsScopedToRuntimeBundleAndVersion()
    {
        var manifest = new ExperienceManifest(
            3101,
            "1.0.0",
            3111,
            [
                new ExperienceBundleRequest(
                    3101,
                    Convert.ToBase64String([1, 2, 3]),
                    BundleVersion: "1.0.0")
            ]);

        var source = manifest.CreateConfigurationSource();

        Assert.True(source.TryGetConfiguration(3111, 3101, "1.0.0", out var configuration));
        Assert.Equal(new byte[] { 1, 2, 3 }, configuration.ToArray());
        Assert.False(source.TryGetConfiguration(3112, 3101, "1.0.0", out _));
        Assert.False(source.TryGetConfiguration(3111, 3101, "2.0.0", out _));
    }

    [Fact]
    public void SerializedManifest_RehydratesArtifactIdentity()
    {
        var hash = new string('a', 64);
        var json =
            $$"""{"experienceId":3101,"version":"1.0.0","runtimeId":3111,"bundles":[{"bundleId":3101,"configurationBase64":"","artifactVersion":"1.0.0","contentHash":"{{hash}}"}]}""";

        var manifest = ExperienceManifest.Parse(Encoding.UTF8.GetBytes(json));

        Assert.True(manifest.UsesRepositoryArtifacts);
        Assert.Equal("1.0.0", manifest.Bundles[0].ArtifactVersion);
        Assert.Equal(hash, manifest.Bundles[0].ContentHash);
    }

    [Fact]
    public void CanonicalGuiSurface_IsConsumableWithoutAnyAppSpecificBundleType()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());

        Assert.True(runtime.TryGetBundle<IMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var bundle));
        Assert.NotNull(bundle);
        Assert.True(MicroBundleGuiSurface.TryGetRoot(bundle!, out var root));
        Assert.NotNull(root);
        Assert.Equal("moniker-root", root!.Id);
    }

    [Fact]
    public void SerializedLegacyManifest_RehydratesRuntimeManifest()
    {
        var json = "{\"experienceId\":3101,\"version\":\"1.0.0\",\"runtimeId\":3111,\"bundles\":[{\"bundleId\":3101,\"configurationBase64\":\"\"}]}";
        var manifest = ExperienceManifest.Parse(Encoding.UTF8.GetBytes(json));
        Assert.Equal(3101UL, manifest.ExperienceId);
        Assert.Equal("1.0.0", manifest.Version);
        var runtimeManifest = manifest.ToRuntimeManifest();
        Assert.Equal(3111UL, runtimeManifest.RuntimeId);
        Assert.Single(runtimeManifest.Bundles);
        Assert.Equal(3101UL, runtimeManifest.Bundles[0].BundleId);
    }
    [Fact]
    public void SemanticExchange_ResolvesIntentThroughProtocolAndGrammar()
    {
        var intent = WorkshopSemanticExchange.OpenForgeIntent;

        var reference = WorkshopSemanticExchange.Resolve(intent);

        Assert.Equal(WorkshopSemanticExchange.IntentProtocolId, reference.ProtocolId);
        Assert.Equal(WorkshopSemanticExchange.OpenForgeSymbolId, reference.SymbolId);
        Assert.True(WorkshopSemanticExchange.IsAllowed(intent));
        Assert.Equal("open.forge", WorkshopSemanticExchange.IntentProtocol.Decode(reference));
    }

    [Fact]
    public void ExperienceManifest_CarriesSemanticIntent()
    {
        var manifest = new ExperienceManifest(
            ForgeMicroBundle.ExperienceId,
            "1.0.0",
            ForgeMicroBundle.RuntimeId,
            [new ExperienceBundleRequest(ForgeMicroBundle.BundleId, "")],
            WorkshopSemanticExchange.OpenForgeIntent);

        var parsed = ExperienceManifest.Parse(
            Encoding.UTF8.GetBytes(
                "{\"experienceId\":3201,\"version\":\"1.0.0\",\"runtimeId\":3211," +
                "\"intent\":{\"name\":\"open.forge\",\"protocolId\":4200}," +
                "\"bundles\":[{\"bundleId\":3201,\"configurationBase64\":\"\"}]}"));

        Assert.Equal(manifest.Intent, parsed.Intent);
        Assert.Equal("open.forge", parsed.Intent!.Name);
        Assert.Equal(WorkshopSemanticExchange.IntentProtocolId, parsed.Intent.ProtocolId);
        Assert.True(WorkshopSemanticExchange.IsAllowed(parsed.Intent));
    }

}
