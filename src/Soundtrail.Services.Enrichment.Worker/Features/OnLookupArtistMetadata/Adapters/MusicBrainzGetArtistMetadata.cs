using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Adapters;

public sealed class MusicBrainzGetArtistMetadata(HttpClient httpClient) : IGetArtistMetadata
{
    public async Task<ArtistMetadata?> GetMetadataAsync(
        string artistName,
        string? sourceArtistId,
        CancellationToken cancellationToken)
    {
        var artist = !string.IsNullOrWhiteSpace(sourceArtistId)
            ? await LookupByIdAsync(sourceArtistId, cancellationToken)
            : await SearchByNameAsync(artistName, cancellationToken);

        return artist is null
            ? null
            : new ArtistMetadata(artist.Name ?? artistName, artist.Id);
    }

    private async Task<MusicBrainzArtistDto?> LookupByIdAsync(string artistId, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<MusicBrainzArtistDto>(
            $"/ws/2/artist/{Uri.EscapeDataString(artistId)}?fmt=json",
            cancellationToken);

    private async Task<MusicBrainzArtistDto?> SearchByNameAsync(string artistName, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"artist:\"{artistName}\"");
        var response = await httpClient.GetFromJsonAsync<MusicBrainzArtistSearchResponse>(
            $"/ws/2/artist?fmt=json&limit=5&query={query}",
            cancellationToken);

        return (response?.Artists ?? [])
            .OrderByDescending(x => Score(x, artistName))
            .FirstOrDefault();
    }

    private static int Score(MusicBrainzArtistDto artist, string expectedArtist)
    {
        var score = int.TryParse(artist.Score, out var parsed) ? parsed / 10 : 0;
        if (MusicMetadataLookupMatch.Normalize(artist.Name) == MusicMetadataLookupMatch.Normalize(expectedArtist))
        {
            score += 100;
        }

        return score;
    }

    private sealed class MusicBrainzArtistSearchResponse
    {
        [JsonPropertyName("artists")]
        public List<MusicBrainzArtistDto>? Artists { get; init; }
    }

    private sealed class MusicBrainzArtistDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("score")]
        public string? Score { get; init; }
    }
}
