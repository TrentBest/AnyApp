using FluentAssertions;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class AnyAppLaunchRequestTests
{
    [Fact]
    public void ParsesExperienceDeepLink()
    {
        var request = AnyAppLaunchRequest.TryParse(
            "anyapp://experience/3201/1.0.0/ABC123?token=session-token");

        request.Should().NotBeNull();
        request!.ExperienceId.Should().Be(3201);
        request.Version.Should().Be("1.0.0");
        request.ContentHash.Should().Be("ABC123");
        request.LaunchToken.Should().Be("session-token");
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
        AnyAppLaunchRequest.TryParse(value).Should().BeNull();
    }
}
