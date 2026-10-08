using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppLaunchRequestTests
{
    [Fact]
    public void ParsesExperienceDeepLink()
    {
        var request = AnyAppLaunchRequest.TryParse(
            "anyapp://experience/3201/1.0.0/ABC123?token=session-token");

        Assert.NotNull(request);
        Assert.Equal((ulong)3201, request!.ExperienceId);
        Assert.Equal("1.0.0", request.Version);
        Assert.Equal("ABC123", request.ContentHash);
        Assert.Equal("session-token", request.LaunchToken);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://example.com/experience/3201/1.0.0/hash?token=x")]
    [InlineData("anyapp://experience/3201/1.0.0/hash")]
    [InlineData("anyapp://experience/not-an-id/1.0.0/hash?token=x")]
    [InlineData("anyapp://other/3201/1.0.0/hash?token=x")]
    public void RejectsMalformedDeepLinks(string? value)
    {
        Assert.Null(AnyAppLaunchRequest.TryParse(value));
    }
}
