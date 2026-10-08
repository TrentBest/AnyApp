using System.Text;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppRuntimeTests
{
    [Fact]
    public void Compose_ProducesMonikerExperienceThroughCos()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.Equal(MonikerExperience.RuntimeId, runtime.RuntimeId);
        Assert.Single(runtime.Bundles);
        Assert.Equal(0, runtime.ArbitrationRounds);
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(MonikerMicroBundle.BundleId, out var bundle));
        Assert.NotNull(bundle);
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
        var surface = Assert.IsAssignableFrom<IAnyAppSurface>(bundle);
        Assert.Equal("moniker-root", surface.Root.Id);
        Assert.Equal("Panel", surface.Root.Kind);
        Assert.True(surface.Root.TryFind("moniker-singularity", out var word));
        Assert.Equal("Row", word!.Kind);
        Assert.Contains(word.Children, child => child.Text == "S" && !child.Properties.ContainsKey("wavePeriod"));
    }

    [Fact]
    public void ExperienceExecution_AddsWavePresentationWithoutMutatingBundle()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(MonikerMicroBundle.BundleId, out var bundle));
        var surface = Assert.IsAssignableFrom<IAnyAppSurface>(bundle);
        var presented = MonikerExperience.ExecutePresentation(surface.Root);
        Assert.True(presented.TryFind("moniker-singularity-0", out var glyph));
        Assert.Equal("S", glyph!.Text);
        Assert.Equal("3.8", glyph.Properties["wavePeriod"]);
        Assert.Equal("7", glyph.Properties["waveAmplitude"]);
        Assert.False(surface.Root.Find("moniker-singularity-0").Properties.ContainsKey("wavePeriod"));
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
