using System.Text;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.FSM_UserIO;
using TheSingularityWorkshop.Experiences.Moniker;
using TheSingularityWorkshop.Experiences.Moniker.Motion.Wave;
using TheSingularityWorkshop.Renderer;
using TheSingularityWorkshop.MicroBundleDomain;
using Xunit;
#if WORKSHOP_STORYBUILDER
using StoryBuilder.MicroBundle;
#endif

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

#if WORKSHOP_STORYBUILDER
    [Fact]
    public void Catalog_ResolvesStoryBuilderAuthoringBundleForComposition()
    {
        var catalog = new AnyAppMicroBundleCatalog();

        Assert.True(catalog.TryResolve(
            StoryBuilderAuthoringBundle.BundleId,
            StoryBuilderAuthoringBundle.BundleVersion,
            out var bundle));
        Assert.IsType<StoryBuilderAuthoringBundle>(bundle);
    }
#endif

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
    public async Task ComposeAsync_LocalExperienceManifest_ComposesThroughCos()
    {
        var experience = new ExperienceManifest(
            MonikerExperience.ExperienceId,
            "1.0.0",
            MonikerExperience.RuntimeId,
            [
                new ExperienceBundleRequest(MonikerMicroBundle.BundleId, "", BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(MonikerWaveMotionMicroBundle.BundleId, "", BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(RendererMicroBundle.BundleId, "", BundleVersion: new RendererMicroBundle().Descriptor.Version)
            ]);

        var runtime = await AnyAppRuntime.ComposeAsync(
            experience,
            new Uri("http://localhost:5000/"));

        Assert.Equal(MonikerExperience.RuntimeId, runtime.RuntimeId);
        Assert.Equal(3, runtime.Bundles.Count);
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var moniker));
        Assert.NotNull(moniker);
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
    public void Manifest_ComposesStartupMonikerSeparatelyFromDestination()
    {
        var manifest = new ExperienceManifest(
            ForgeMicroBundle.ExperienceId,
            "1.0.0",
            ForgeMicroBundle.RuntimeId,
            [
                new ExperienceBundleRequest(MonikerExperience.MonikerBundleId, "", BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(MonikerExperience.MotionBundleId, "", BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(MonikerExperience.RendererBundleId, "", BundleVersion: "0.1.0-alpha.2"),
                new ExperienceBundleRequest(ForgeMicroBundle.BundleId, "", BundleVersion: "1.0.0")
            ],
            WorkshopSemanticExchange.OpenForgeIntent,
            [MonikerExperience.MonikerBundleId]);

        var startup = manifest.CreateStartupMonikerManifest();

        Assert.NotNull(startup);
        Assert.Equal(new[] { MonikerExperience.MonikerBundleId }, startup!.GetStartupMonikerBundleIds());
        Assert.Contains(startup.Bundles, bundle => bundle.BundleId == MonikerExperience.MonikerBundleId);
        Assert.Contains(startup.Bundles, bundle => bundle.BundleId == MonikerExperience.MotionBundleId);
        Assert.Contains(startup.Bundles, bundle => bundle.BundleId == MonikerExperience.RendererBundleId);
        Assert.DoesNotContain(startup.Bundles, bundle => bundle.BundleId == ForgeMicroBundle.BundleId);
        Assert.Null(startup.Intent);
    }

    [Fact]
    public void LegacyManifest_InfersWorkshopMonikerOnlyWhenIncluded()
    {
        var withMoniker = new ExperienceManifest(
            3201,
            "1.0.0",
            3211,
            [new ExperienceBundleRequest(MonikerExperience.MonikerBundleId, "")]);
        var withoutMoniker = new ExperienceManifest(
            3201,
            "1.0.0",
            3211,
            [new ExperienceBundleRequest(ForgeMicroBundle.BundleId, "")]);

        Assert.Equal(new[] { MonikerExperience.MonikerBundleId }, withMoniker.GetStartupMonikerBundleIds());
        Assert.Empty(withoutMoniker.GetStartupMonikerBundleIds());
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
    public void SemanticExchange_IsAllowedReturnsFalseForUnknownOrForeignIntents()
    {
        Assert.False(WorkshopSemanticExchange.IsAllowed(
            new SemanticIntent("open.forge", 9999)));
        Assert.False(WorkshopSemanticExchange.IsAllowed(
            new SemanticIntent("not.a.known.intent", WorkshopSemanticExchange.IntentProtocolId)));
        Assert.True(WorkshopSemanticExchange.IsAllowed(WorkshopSemanticExchange.ExploreExperiencesIntent));
    }

    [Fact]
    public void SemanticExchange_ResolveRejectsUnknownOrForeignIntents()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkshopSemanticExchange.Resolve(new SemanticIntent("open.forge", 9999)));
        Assert.Throws<KeyNotFoundException>(() =>
            WorkshopSemanticExchange.Resolve(
                new SemanticIntent("not.a.known.intent", WorkshopSemanticExchange.IntentProtocolId)));
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

    [Fact]
    public void ExperienceManifest_LoadFileResolvesPerBundleConfigurationRelativeToManifest()
    {
        var root = Path.Combine(Path.GetTempPath(), "AnyAppManifestTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "configuration"));
        try
        {
            File.WriteAllText(
                Path.Combine(root, "manifest.json"),
                "{\"experienceId\":3101,\"version\":\"1.0.0-dev\",\"runtimeId\":3111," +
                "\"bundles\":[{\"bundleId\":3101,\"bundleVersion\":\"1.0.0\"," +
                "\"configurationPath\":\"configuration/3101.json\"}]}");
            File.WriteAllText(
                Path.Combine(root, "configuration", "3101.json"),
                "{\"webBridgeEndpoint\":\"http://127.0.0.1:48156/\"}");

            var manifest = ExperienceManifest.LoadFile(Path.Combine(root, "manifest.json"));
            var source = manifest.CreateConfigurationSource();

            Assert.True(source.TryGetConfiguration(3111, 3101, "1.0.0", out var configuration));
            Assert.Contains("webBridgeEndpoint", Encoding.UTF8.GetString(configuration.Span));
            Assert.False(source.TryGetConfiguration(9999, 3101, "1.0.0", out _));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExperienceManifest_RejectsConfigurationPathsOutsideManifestDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "AnyAppManifestTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(
                Path.Combine(root, "manifest.json"),
                "{\"experienceId\":3101,\"version\":\"1.0.0-dev\",\"runtimeId\":3111," +
                "\"bundles\":[{\"bundleId\":3101,\"bundleVersion\":\"1.0.0\"," +
                "\"configurationPath\":\"../outside.json\"}]}");

            var manifest = ExperienceManifest.LoadFile(Path.Combine(root, "manifest.json"));

            Assert.Throws<InvalidOperationException>(() => manifest.CreateConfigurationSource());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

#if WORKSHOP_USE_REPOSITORY_SOURCES
    [Fact]
    public void ResolvedExperienceManifest_MapsFlatClosureAndPublishedArbitrationIntoFsmCos()
    {
        var manifest = new ExperienceManifest(
            9001,
            "1.0.0",
            9002,
            [
                new ExperienceBundleRequest(
                    1,
                    ArtifactVersion: "1.0.0",
                    ContentHash: "sha256:bundle-1",
                    BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(
                    2,
                    ArtifactVersion: "1.0.0",
                    ContentHash: "sha256:bundle-2",
                    BundleVersion: "1.0.0")
            ],
            ResolvedPlan: new ExperienceResolvedPlan(
                1,
                [1, 2],
                [new ExperienceResolvedDependency(2, 1)]));

        var runtimeManifest = manifest.ToRuntimeManifest();

        Assert.True(runtimeManifest.ArbitrationResolved);
        Assert.Equal(1, runtimeManifest.PublishedArbitrationRounds);
        Assert.Equal(new ulong[] { 1, 2 }, runtimeManifest.LoadPlan!.Select(entry => entry.Reference.BundleId));
        Assert.Equal(new[] { new RuntimeManifestDependency(2, 1) }, runtimeManifest.Schedule!.Dependencies);
    }
#endif

#if WORKSHOP_USE_REPOSITORY_SOURCES
    [Fact]
    public void OneAnyAppHostComposesDifferentExperiencesFromDifferentManifests()
    {
        var forgeManifest = new ExperienceManifest(
            3201,
            "1.0.0",
            3211,
            [new ExperienceBundleRequest(ForgeMicroBundle.BundleId, BundleVersion: "1.0.0")]);
        var monikerManifest = new ExperienceManifest(
            3101,
            "1.0.0",
            3111,
            [
                new ExperienceBundleRequest(MonikerExperience.MonikerBundleId, BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(MonikerExperience.MotionBundleId, BundleVersion: "1.0.0"),
                new ExperienceBundleRequest(MonikerExperience.RendererBundleId, BundleVersion: "0.1.0-alpha.2")
            ]);

        var forgeAssembly = AnyAppRuntime.Compose(forgeManifest.ToRuntimeManifest());
        var monikerAssembly = AnyAppRuntime.Compose(monikerManifest.ToRuntimeManifest());

        Assert.Equal(new ulong[] { ForgeMicroBundle.BundleId }, forgeAssembly.Bundles.Select(bundle => bundle.Id));
        Assert.Equal(
            new ulong[]
            {
                MonikerExperience.MonikerBundleId,
                MonikerExperience.MotionBundleId,
                MonikerExperience.RendererBundleId
            },
            monikerAssembly.Bundles.Select(bundle => bundle.Id));
    }
#endif

#if WORKSHOP_USE_REPOSITORY_SOURCES
    [Fact]
    public void StartupMonikerKeepsOnlyItsTransitiveResolvedClosure()
    {
        static ExperienceBundleRequest Bundle(ulong id, string version = "1.0.0") =>
            new(
                id,
                ArtifactVersion: version,
                ContentHash: new string('a', 64),
                BundleVersion: version,
                ConfigurationArtifactVersion: $"configuration-{version}",
                ConfigurationContentHash: new string('b', 64));

        var manifest = new ExperienceManifest(
            3101,
            "1.0.0",
            3111,
            [
                Bundle(4001),
                Bundle(MonikerExperience.MonikerBundleId),
                Bundle(MonikerExperience.MotionBundleId),
                Bundle(MonikerExperience.RendererBundleId, "0.1.0-alpha.2"),
                Bundle(5000)
            ],
            StartupMonikerBundleIds: [MonikerExperience.MonikerBundleId],
            ResolvedPlan: new ExperienceResolvedPlan(
                1,
                [4001, MonikerExperience.MonikerBundleId, MonikerExperience.MotionBundleId,
                    MonikerExperience.RendererBundleId, 5000],
                [
                    new ExperienceResolvedDependency(MonikerExperience.MonikerBundleId, 4001),
                    new ExperienceResolvedDependency(5000, 4001)
                ]));

        var startup = manifest.CreateStartupMonikerManifest();
        Assert.NotNull(startup);
        Assert.NotNull(startup!.ResolvedPlan);
        Assert.Equal(
            new ulong[]
            {
                4001,
                MonikerExperience.MonikerBundleId,
                MonikerExperience.MotionBundleId,
                MonikerExperience.RendererBundleId
            },
            startup.ResolvedPlan!.LoadOrder);
        Assert.Single(startup.ResolvedPlan.Dependencies);

        var runtimeManifest = startup.ToRuntimeManifest();
        Assert.True(runtimeManifest.ArbitrationResolved);
        Assert.Equal(startup.ResolvedPlan.LoadOrder, runtimeManifest.LoadPlan!.Select(entry => entry.Reference.BundleId));
    }
#endif

}
