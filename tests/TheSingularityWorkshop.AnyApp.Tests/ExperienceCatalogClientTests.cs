using System.Net;
using System.Text;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class ExperienceCatalogClientTests
{
    private const string ValidHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public async Task ListAsyncReturnsValidatedPublications()
    {
        using var client = CreateClient($"[{{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":\"{ValidHash}\"}}]");

        var publications = await client.ListAsync();

        var publication = Assert.Single(publications);
        Assert.Equal(41UL, publication.ExperienceId);
        Assert.Equal("1.0.0", publication.Version);
        Assert.Equal(ValidHash, publication.ContentHash);
    }

    [Fact]
    public async Task ListAsyncTreatsNullCatalogAsEmpty()
    {
        using var client = CreateClient("null");

        Assert.Empty(await client.ListAsync());
    }

    [Theory]
    [InlineData("{\"experienceId\":0,\"version\":\"1.0.0\",\"contentHash\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"}")]
    [InlineData("{\"experienceId\":41,\"version\":\" \",\"contentHash\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"}")]
    [InlineData("{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":\"short\"}")]
    [InlineData("{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":null}")]
    [InlineData("{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg\"}")]
    public async Task ListAsyncRejectsMalformedPublication(string publication)
    {
        using var client = CreateClient($"[{publication}]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListAsync());
    }

    [Fact]
    public async Task ListAsyncRejectsNullPublicationEntries()
    {
        using var client = CreateClient("[null]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListAsync());
    }

    [Fact]
    public async Task ListAsyncRejectsDuplicateImmutablePublications()
    {
        var publication = $"{{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":\"{ValidHash}\"}}";
        using var client = CreateClient($"[{publication},{publication}]");

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ListAsync());
    }

    [Fact]
    public async Task ListAsyncAllowsMultipleVersionsOfOneExperience()
    {
        var first = $"{{\"experienceId\":41,\"version\":\"1.0.0\",\"contentHash\":\"{ValidHash}\"}}";
        var second = $"{{\"experienceId\":41,\"version\":\"2.0.0\",\"contentHash\":\"{ValidHash}\"}}";
        using var client = CreateClient($"[{first},{second}]");

        var publications = await client.ListAsync();

        Assert.Equal(2, publications.Count);
        Assert.Equal(new[] { "1.0.0", "2.0.0" }, publications.Select(item => item.Version));
    }

    private static ExperienceCatalogClient CreateClient(string response)
    {
        var handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://experiences.test/")
        };

        return new ExperienceCatalogClient(httpClient);
    }

    private sealed class StubHttpMessageHandler(string response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("https://experiences.test/api/experiences", request.RequestUri?.AbsoluteUri);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        }
    }
}
