using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppWebBridgeTests
{
    [Fact]
    public void HostEndpointConfigurationIsIndependentOfExperienceBundleOverrides()
    {
        using var bridge = new AnyAppWebBridge();

        bridge.ConfigureEndpoint(new Uri("http://127.0.0.1:48157/custom-path?ignored=true"));

        Assert.Equal("http://127.0.0.1:48157/", bridge.Endpoint.AbsoluteUri);
    }

    [Fact]
    public void HostEndpointConfigurationRejectsNonLoopbackEndpoints()
    {
        using var bridge = new AnyAppWebBridge();

        Assert.Throws<ArgumentException>(() =>
            bridge.ConfigureEndpoint(new Uri("https://example.com/")));
        Assert.Throws<ArgumentException>(() =>
            bridge.ConfigureEndpoint(new Uri("http://192.0.2.10:48156/")));
    }
}
