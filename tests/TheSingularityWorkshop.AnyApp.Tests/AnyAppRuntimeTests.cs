using System.Text;
using TheSingularityWorkshop.FSM_COS;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppRuntimeTests
{
    [Fact]
    public void Compose_ProducesMonikerExperienceThroughCos()
    {
        var runtime = AnyAppRuntime.Compose(MonikerExperience.CreateManifest());
        Assert.Equal(MonikerExperience.RuntimeId, runtime.RuntimeId);
        Assert.Equal(1, runtime.Bundles.Count);
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
    public void SerializedManifest_RehydratesRuntimeManifest()
    {
        var json = "{\"experienceId\":3101,\"version\":\"1.0.0\",\"runtimeId\":3111,\"bundles\":[{\"bundleId\":3101,\"configurationBase64\":\"\"}]}";
        var manifest = ExperienceManifest.Parse(Encoding.UTF8.GetBytes(json));
        Assert.Equal(3101UL, manifest.ExperienceId);
        Assert.Equal("1.0.0", manifest.Version);
        var runtimeManifest = manifest.ToRuntimeManifest();
        Assert.Equal(3111UL, runtimeManifest.RuntimeId);
        Assert.Equal(2, runtimeManifest.Bundles.Count);
        Assert.Equal(3101UL, runtimeManifest.Bundles[0].BundleId);
        Assert.Equal(3101UL, runtimeManifest.Bundles[1].BundleId);
    }

    [Fact]
    public void ExperienceManifest_AlwaysPrependsMonikerBootstrap()
    {
        var manifest = new ExperienceManifest(
            ForgeMicroBundle.ExperienceId,
            "1.0.0",
            ForgeMicroBundle.RuntimeId,
            [new ExperienceBundleRequest(ForgeMicroBundle.BundleId, "")]);

        var runtimeManifest = manifest.ToRuntimeManifest();

        Assert.Equal(2, runtimeManifest.Bundles.Count);
        Assert.Equal(MonikerMicroBundle.BundleId, runtimeManifest.Bundles[0].BundleId);
        Assert.Equal(ForgeMicroBundle.BundleId, runtimeManifest.Bundles[1].BundleId);
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
