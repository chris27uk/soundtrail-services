using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;

public sealed class OdesliStreamingReferences(HttpClient httpClient, IOptions<OdesliOptions> options, IClockPort clockPort) : IGetMusicTrackReference
{
    private readonly OdesliOptions options = options.Value;

    public async Task<IReadOnlyList<StreamingLocation>> GetStreamingLocations(LookupCriteria searchCriteria, CancellationToken cancellationToken)
    {
        var requestUri = BuildRequestUri(searchCriteria);
        var response = await httpClient.GetFromJsonAsync<OdesliLookupResponse>(requestUri, cancellationToken);

        if (response?.LinksByPlatform is null)
        {
            return [];
        }

        var references = new List<StreamingLocation>();
        if (TryBuildYouTubeReference(response.LinksByPlatform, out var youTubeReference))
        {
            references.Add(youTubeReference);
        }
        if (TryBuildSpotifyReference(response.LinksByPlatform, out var spotifyReference))
        {
            references.Add(spotifyReference);
        }
        if (TryBuildAppleReference(response.LinksByPlatform, out var appleReference))
        {
            references.Add(appleReference);
        }
        return references;
    }

    private string BuildRequestUri(LookupCriteria searchCriteria)
    {
        return searchCriteria.Match(
            query => throw new InvalidOperationException($"Streaming locations lookup does not support unified search queries."),
            (track, artist, album) => $"/v1-user/links?title={Uri.EscapeDataString(track)}&artist={Uri.EscapeDataString(artist)}&album={Uri.EscapeDataString(album ?? string.Empty)}&userCountry={Uri.EscapeDataString(options.UserCountry)}",
            isrc => $"/v1-user/links?id={Uri.EscapeDataString(isrc ?? string.Empty)}&platform=isrc&userCountry={Uri.EscapeDataString(options.UserCountry)}");
    }

    public static void ConfigureHttpClient(HttpClient httpClient, OdesliOptions options) => httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);


    private sealed class OdesliLookupResponse
    {
        [JsonPropertyName("linksByPlatform")]
        public Dictionary<string, OdesliPlatformLinkDto>? LinksByPlatform { get; init; }
    }

    public sealed class OdesliPlatformLinkDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}
