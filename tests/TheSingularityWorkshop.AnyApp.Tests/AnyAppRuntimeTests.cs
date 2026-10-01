using TheSingularityWorkshop.FSM_COS;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppRuntimeTests
{
    [Fact]
    public void Compose_ProducesRuntimeAssemblyThroughCos()
    {
        var runtime = AnyAppRuntime.Compose();

        Assert.Equal(AnyAppRuntime.RuntimeId, runtime.RuntimeId);
        Assert.Equal(1, runtime.Bundles.Count);
        Assert.Equal(0, runtime.ArbitrationRounds);
        Assert.True(runtime.TryGetBundle<AnyAppMicroBundle>(
            AnyAppMicroBundle.BundleId,
            out var bundle));
        Assert.NotNull(bundle);
    }

    [Fact]
    public void ComposedBundle_ExposesSemanticGuiSurface()
    {
        var runtime = AnyAppRuntime.Compose();

        Assert.True(runtime.TryGetBundle<IAnyAppSurface>(
            AnyAppMicroBundle.BundleId,
            out var surface));
        Assert.NotNull(surface);
        Assert.Equal("anyapp-root", surface!.Root.Id);
        Assert.Equal("Column", surface.Root.Kind);
        Assert.Contains(
            surface.Root.Children,
            child => child.Id == "title" && child.Text == "AnyApp");
    }
}