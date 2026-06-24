using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;

public sealed class OdesliStreamingReferences(
    HttpClient httpClient,
    IOptions<OdesliOptions> options) : IGetMusicTrackReference
{
    private readonly OdesliOptions options = options.Value;

    public async Task<IReadOnlyList<ExternalReference>> GetReferenceToMusicTrack(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildRequestUri(searchTerm);
        var response = await httpClient.GetFromJsonAsync<OdesliLookupResponse>(
            requestUri,
            cancellationToken);

        if (response?.LinksByPlatform is null)
        {
            return [];
        }

        var references = new List<ExternalReference>();

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

    private string BuildRequestUri(MusicSearchTerm searchTerm)
    {
        return searchTerm.Match(
            query =>
                throw new InvalidOperationException($"Streaming locations lookup does not support unified search queries. Query='{query}'."),
            (track, artist, album) =>
                $"/v1-user/links?title={Uri.EscapeDataString(track)}&artist={Uri.EscapeDataString(artist)}&album={Uri.EscapeDataString(album ?? string.Empty)}&userCountry={Uri.EscapeDataString(options.UserCountry)}",
            isrc =>
                $"/v1-user/links?id={Uri.EscapeDataString(isrc ?? string.Empty)}&platform=isrc&userCountry={Uri.EscapeDataString(options.UserCountry)}");
    }

    public static void ConfigureHttpClient(HttpClient httpClient, OdesliOptions options) =>
        httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);

    private static bool TryBuildYouTubeReference(
        IReadOnlyDictionary<string, OdesliPlatformLinkDto> linksByPlatform,
        out ExternalReference reference)
    {
        if (!linksByPlatform.TryGetValue("youtubeMusic", out var youtubeMusic)
            || string.IsNullOrWhiteSpace(youtubeMusic.Url))
        {
            reference = null!;
            return false;
        }

        var uri = new Uri(youtubeMusic.Url, UriKind.Absolute);
        var videoId = GetQueryParameter(uri, "v");

        if (string.IsNullOrWhiteSpace(videoId))
        {
            reference = null!;
            return false;
        }

        reference = new ExternalReference(
            ProviderName.YoutubeMusic,
            uri,
            videoId);
        return true;
    }

    private static bool TryBuildSpotifyReference(
        IReadOnlyDictionary<string, OdesliPlatformLinkDto> linksByPlatform,
        out ExternalReference reference)
    {
        if (!linksByPlatform.TryGetValue("spotify", out var spotify)
            || string.IsNullOrWhiteSpace(spotify.Url))
        {
            reference = null!;
            return false;
        }

        var uri = new Uri(spotify.Url, UriKind.Absolute);
        var trackSegment = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(segment => !string.Equals(segment, "track", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(trackSegment))
        {
            reference = null!;
            return false;
        }

        reference = new ExternalReference(
            ProviderName.Spotify,
            uri,
            trackSegment);
        return true;
    }

    private static bool TryBuildAppleReference(
        IReadOnlyDictionary<string, OdesliPlatformLinkDto> linksByPlatform,
        out ExternalReference reference)
    {
        if (!linksByPlatform.TryGetValue("appleMusic", out var appleMusic)
            || string.IsNullOrWhiteSpace(appleMusic.Url))
        {
            reference = null!;
            return false;
        }

        var uri = new Uri(appleMusic.Url, UriKind.Absolute);
        var trackId = GetQueryParameter(uri, "i");

        if (string.IsNullOrWhiteSpace(trackId))
        {
            reference = null!;
            return false;
        }

        reference = new ExternalReference(
            ProviderName.AppleMusic,
            uri,
            trackId);
        return true;
    }

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

    private static string? GetQueryParameter(Uri uri, string key)
    {
        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in query)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }
}
