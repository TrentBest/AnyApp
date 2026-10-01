using TheSingularityWorkshop.FSM_COS;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppRuntimeTests
{
    [Fact]
    public void Compose_ProducesMonikerExperienceThroughCos()
    {
        var runtime = AnyAppRuntime.Compose();

        Assert.Equal(MonikerExperience.RuntimeId, runtime.RuntimeId);
        Assert.Equal(1, runtime.Bundles.Count);
        Assert.Equal(0, runtime.ArbitrationRounds);
        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var bundle));
        Assert.NotNull(bundle);
    }

    [Fact]
    public void ComposedBundle_ExposesSemanticGuiSurface()
    {
        var runtime = AnyAppRuntime.Compose();

        Assert.True(runtime.TryGetBundle<MonikerMicroBundle>(
            MonikerMicroBundle.BundleId,
            out var bundle));
        Assert.NotNull(bundle);

        var surface = Assert.IsAssignableFrom<IAnyAppSurface>(bundle);

        Assert.Equal("moniker-root", surface.Root.Id);
        Assert.Equal("Panel", surface.Root.Kind);
        Assert.True(surface.Root.TryFind("moniker-singularity", out var word));
        Assert.Equal("Row", word!.Kind);
        Assert.Contains(
            word.Children,
            child => child.Text == "S" && child.Properties.ContainsKey("wavePeriod"));
    }
}
