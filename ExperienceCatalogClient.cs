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

    public async Task<IReadOnlyList<PublishedExperience>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<PublishedExperience>>(
                   "api/experiences",
                   JsonOptions,
                   cancellationToken)
               ?? [];
    }

    public async Task<ExperienceManifest> GetManifestAsync(
        PublishedExperience experience,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(experience);

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
