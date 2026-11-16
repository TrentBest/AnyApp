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

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://127.0.0.1:48156")]
    [InlineData("http://[::1]:5173")]
    public void OriginPolicyRejectsLoopbackOriginsUnlessExplicitlyConfigured(string origin)
    {
        Assert.False(AnyAppWebBridge.IsAllowedOrigin(origin, null));
    }

    [Fact]
    public void OriginPolicyAllowsOnlyTheExactConfiguredOrigin()
    {
        const string allowedOrigin = "http://localhost:5173";

        Assert.True(AnyAppWebBridge.IsAllowedOrigin(allowedOrigin, allowedOrigin));
        Assert.False(AnyAppWebBridge.IsAllowedOrigin("http://localhost:5174", allowedOrigin));
        Assert.False(AnyAppWebBridge.IsAllowedOrigin("https://localhost:5173", allowedOrigin));
        Assert.False(AnyAppWebBridge.IsAllowedOrigin("http://127.0.0.1:5173", allowedOrigin));
    }

    [Theory]
    [InlineData("http://user@localhost:5173")]
    [InlineData("http://localhost:5173/untrusted-path")]
    [InlineData("http://localhost:5173/?token=secret")]
    [InlineData("http://localhost:5173/#fragment")]
    [InlineData("http://localhost:5173?")]
    [InlineData("http://localhost:5173/#")]
    [InlineData("null")]
    [InlineData("")]
    public void OriginPolicyRejectsMalformedOrNonOriginValues(string origin)
    {
        Assert.False(AnyAppWebBridge.IsAllowedOrigin(origin, "http://localhost:5173"));
    }

    [Theory]
    [InlineData("http://localhost:5173/path")]
    [InlineData("http://localhost:5173?query=value")]
    [InlineData("http://localhost:5173/#fragment")]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData("ftp://localhost:5173")]
    public void OriginPolicyIgnoresInvalidAllowlistEntries(string configuredOrigin)
    {
        Assert.False(AnyAppWebBridge.IsAllowedOrigin(
            "http://localhost:5173",
            configuredOrigin));
    }

    [Fact]
    public void OriginPolicyAllowsOnlyTheConfiguredWorkshopDefault()
    {
        Assert.True(AnyAppWebBridge.IsAllowedOrigin(
            "https://lemon-ground-09f542010.1.azurestaticapps.net",
            null));
        Assert.False(AnyAppWebBridge.IsAllowedOrigin("http://localhost:5173", null));
    }
}\n