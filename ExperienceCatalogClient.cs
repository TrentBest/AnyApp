using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

public sealed record PublishedExperience(
    ulong ExperienceId,
    string Version,
    string ContentHash);

public sealed class ExperienceCatalogClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ExperienceCatalogClient(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.IsAbsoluteUri)
            throw new ArgumentException("The Experience repository endpoint must be absolute.", nameof(endpoint));

        _httpClient = new HttpClient
        {
            BaseAddress = EnsureTrailingSlash(endpoint)
        };
    }

    internal ExperienceCatalogClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (httpClient.BaseAddress is null || !httpClient.BaseAddress.IsAbsoluteUri)
            throw new ArgumentException("The HTTP client must have an absolute base address.", nameof(httpClient));

        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PublishedExperience>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var publications = await _httpClient.GetFromJsonAsync<List<PublishedExperience?>>(
            "api/experiences",
            JsonOptions,
            cancellationToken);

        if (publications is null)
            return [];

        var validated = new List<PublishedExperience>(publications.Count);
        var publicationIdentities = new HashSet<(ulong ExperienceId, string Version, string ContentHash)>();

        foreach (var publication in publications)
        {
            if (publication is null)
                throw new InvalidOperationException("The repository returned a null Experience publication.");

            ValidatePublication(publication);

            if (!publicationIdentities.Add((
                    publication.ExperienceId,
                    publication.Version,
                    publication.ContentHash.ToUpperInvariant())))
            {
                throw new InvalidOperationException(
                    $"The repository returned the same immutable publication more than once for Experience {publication.ExperienceId}.");
            }

            validated.Add(publication);
        }

        return validated;
    }

    private static void ValidatePublication(PublishedExperience publication)
    {
        if (publication.ExperienceId == 0)
            throw new InvalidOperationException("The repository returned an Experience publication with ID zero.");

        if (string.IsNullOrWhiteSpace(publication.Version))
            throw new InvalidOperationException(
                $"The repository returned an empty version for Experience {publication.ExperienceId}.");

        if (string.IsNullOrEmpty(publication.ContentHash) ||
            publication.ContentHash.Length != 64 ||
            publication.ContentHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"The repository returned a malformed SHA-256 content hash for Experience {publication.ExperienceId}.");
        }
    }

    public async Task<ExperienceManifest> GetManifestAsync(
        PublishedExperience experience,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(experience);
        ValidatePublication(experience);

        var uri =
            $"api/experiences/{experience.ExperienceId}/{Uri.EscapeDataString(experience.Version)}/{experience.ContentHash}";

        var artifact = await _httpClient.GetFromJsonAsync<ExperienceArtifactDto>(
            uri,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("The repository returned an empty Experience artifact response.");

        if (artifact.ExperienceId != experience.ExperienceId ||
            !string.Equals(artifact.Version, experience.Version, StringComparison.Ordinal) ||
            !string.Equals(artifact.ContentHash, experience.ContentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The repository returned an Experience artifact with a different identity.");
        }

        return ExperienceManifest.Parse(Convert.FromBase64String(artifact.ContentBase64));
    }

    public void Dispose() => _httpClient.Dispose();

    private static Uri EnsureTrailingSlash(Uri uri) =>
        uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? uri
            : new Uri(uri.AbsoluteUri + "/", UriKind.Absolute);

    private sealed record ExperienceArtifactDto(
        ulong ExperienceId,
        string Version,
        string ContentHash,
        string ContentBase64);
}
