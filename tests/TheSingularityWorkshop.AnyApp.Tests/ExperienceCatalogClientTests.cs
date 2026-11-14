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


    [Fact]
    public async Task GetManifestAsyncRejectsArtifactWhenContentBytesDoNotMatchPublishedHash()
    {
        var content = Encoding.UTF8.GetBytes("{\"experienceId\":41,\"version\":\"1.0.0\",\"runtimeId\":7,\"bundles\":[]}");
        var publishedHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes("different bytes")));
        var publication = new PublishedExperience(41, "1.0.0", publishedHash);
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            experienceId = 41,
            version = "1.0.0",
            contentHash = publishedHash,
            contentBase64 = Convert.ToBase64String(content)
        });
        using var client = CreateArtifactClient(response, publication);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetManifestAsync(publication));

        Assert.Contains("do not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(42, "1.0.0")]
    [InlineData(41, "2.0.0")]
    public async Task GetManifestAsyncRejectsManifestWithDifferentPublicationIdentity(
        ulong manifestExperienceId,
        string manifestVersion)
    {
        var content = Encoding.UTF8.GetBytes(
            $"{{\"experienceId\":{manifestExperienceId},\"version\":\"{manifestVersion}\",\"runtimeId\":7,\"bundles\":[]}}");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        var publication = new PublishedExperience(41, "1.0.0", hash);
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            experienceId = publication.ExperienceId,
            version = publication.Version,
            contentHash = hash,
            contentBase64 = Convert.ToBase64String(content)
        });
        using var client = CreateArtifactClient(response, publication);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetManifestAsync(publication));

        Assert.Contains("manifest identity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestAsyncAcceptsArtifactWhenContentBytesMatchPublishedHash()
    {
        var content = Encoding.UTF8.GetBytes("{\"experienceId\":41,\"version\":\"1.0.0\",\"runtimeId\":7,\"bundles\":[]}");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content));
        var publication = new PublishedExperience(41, "1.0.0", hash);
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            experienceId = 41,
            version = "1.0.0",
            contentHash = hash,
            contentBase64 = Convert.ToBase64String(content)
        });
        using var client = CreateArtifactClient(response, publication);

        var manifest = await client.GetManifestAsync(publication);

        Assert.Equal(41UL, manifest.ExperienceId);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal(7UL, manifest.RuntimeId);
    }


    [Fact]
    public async Task GetManifestAsyncPropagatesCancellation()
    {
        var publication = new PublishedExperience(41, "1.0.0", ValidHash);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var client = CreateArtifactClient(
            (request, token) => Task.FromCanceled<HttpResponseMessage>(token),
            publication);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetManifestAsync(publication, cancellation.Token));
    }

    [Fact]
    public async Task GetManifestAsyncRejectsNonSuccessHttpStatus()
    {
        var publication = new PublishedExperience(41, "1.0.0", ValidHash);
        using var client = CreateArtifactClient(
            (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            publication);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetManifestAsync(publication));
    }

    [Fact]
    public async Task GetManifestAsyncRejectsEmptyArtifactResponse()
    {
        var publication = new PublishedExperience(41, "1.0.0", ValidHash);
        using var client = CreateArtifactClient(
            (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            }),
            publication);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetManifestAsync(publication));

        Assert.Contains("empty Experience artifact", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetManifestAsyncRejectsInvalidBase64ArtifactContent()
    {
        var publication = new PublishedExperience(41, "1.0.0", ValidHash);
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            experienceId = publication.ExperienceId,
            version = publication.Version,
            contentHash = publication.ContentHash,
            contentBase64 = "not base64!"
        });
        using var client = CreateArtifactClient(
            (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            }),
            publication);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetManifestAsync(publication));

        Assert.Contains("invalid Base64", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData(42, "1.0.0", "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData(41, "2.0.0", "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData(41, "1.0.0", "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")]
    public async Task GetManifestAsyncRejectsArtifactWithDifferentIdentity(
        ulong artifactExperienceId,
        string artifactVersion,
        string artifactHash)
    {
        var publication = new PublishedExperience(41, "1.0.0", ValidHash);
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            experienceId = artifactExperienceId,
            version = artifactVersion,
            contentHash = artifactHash,
            contentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
        });
        using var client = CreateArtifactClient(
            (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            }),
            publication);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetManifestAsync(publication));

        Assert.Contains("different identity", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ExperienceCatalogClient CreateArtifactClient(
        string response,
        PublishedExperience publication) =>
        CreateArtifactClient(
            (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            }),
            publication);

    private static ExperienceCatalogClient CreateArtifactClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
        PublishedExperience publication)
    {
        var handler = new ArtifactStubHttpMessageHandler(sendAsync, publication);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://experiences.test/")
        };

        return new ExperienceCatalogClient(httpClient);
    }

    private sealed class ArtifactStubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
        PublishedExperience publication) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var expectedPath =
                $"/api/experiences/{publication.ExperienceId}/{Uri.EscapeDataString(publication.Version)}/{publication.ContentHash}";
            Assert.Equal(expectedPath, request.RequestUri?.AbsolutePath);

            return sendAsync(request, cancellationToken);
        }
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
