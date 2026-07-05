using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Adapters;

public sealed class MusicBrainzGetAlbumMetadata(HttpClient httpClient) : IGetAlbumMetadata
{
    public async Task<Album?> GetMetadataAsync(LookupCriteria criteria, CancellationToken cancellationToken)
    {
        
    }

    private async Task<MusicBrainzReleaseDto?> LookupByIdAsync(string releaseId, CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<MusicBrainzReleaseDto>(
            $"/ws/2/release/{Uri.EscapeDataString(releaseId)}?fmt=json&inc=artist-credits",
            cancellationToken);

    private async Task<MusicBrainzReleaseDto?> SearchByNameAsync(
        string artistName,
        string albumTitle,
        CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"release:\"{albumTitle}\" AND artist:\"{artistName}\"");
        var response = await httpClient.GetFromJsonAsync<MusicBrainzReleaseSearchResponse>(
            $"/ws/2/release?fmt=json&limit=5&query={query}&inc=artist-credits",
            cancellationToken);

        return (response?.Releases ?? [])
            .OrderByDescending(x => Score(x, artistName, albumTitle))
            .FirstOrDefault();
    }

    private static int Score(MusicBrainzReleaseDto release, string expectedArtist, string expectedAlbum)
    {
        var score = int.TryParse(release.Score, out var parsed) ? parsed / 10 : 0;
        if (MusicMetadataLookupMatch.Normalize(release.Title) == MusicMetadataLookupMatch.Normalize(expectedAlbum))
        {
            score += 100;
        }

        if (MusicMetadataLookupMatch.Normalize(release.ArtistCredit?.FirstOrDefault()?.Name) == MusicMetadataLookupMatch.Normalize(expectedArtist))
        {
            score += 100;
        }

        return score;
    }

    private static DateOnly? ParseReleaseDate(string? value) =>
        DateOnly.TryParse(value, out var parsed)
            ? parsed
            : null;

    private sealed class MusicBrainzReleaseSearchResponse
    {
        [JsonPropertyName("releases")]
        public List<MusicBrainzReleaseDto>? Releases { get; init; }
    }

    private sealed class MusicBrainzReleaseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }

        [JsonPropertyName("score")]
        public string? Score { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCreditDto>? ArtistCredit { get; init; }
    }

    private sealed class MusicBrainzArtistCreditDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("artist")]
        public MusicBrainzArtistDto? Artist { get; init; }
    }

    private sealed class MusicBrainzArtistDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }
    }
}
