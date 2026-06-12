using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.TrackLookup;

namespace Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution.Adapters;

public sealed class MusicBrainzGetCanonicalMusicMetadata(HttpClient httpClient) : IGetCanonicalMusicMetadata
{
    public async Task<SongMetadata?> GetMetadataAsync(
        MusicSearchTerm searchTerm,
        CancellationToken cancellationToken)
    {
        var recording = await searchTerm.Match(async (track, artist, album) => await SearchByNamesAsync(
            track,
            artist,
            album,
            cancellationToken), async isrc => await LookupByIsrcAsync(isrc, cancellationToken));

        if (recording is null)
        {
            return null;
        }

        var fallbackTitle = searchTerm.Match((track, _, _) => track, __ => string.Empty);
        var fallbackArtist = searchTerm.Match((_, artist, _) => artist, __ => string.Empty);
        var fallbackIsrc = searchTerm.Match<string?>((_, _, _) => null, isrc => isrc);

        return new SongMetadata(
            recording.Title ?? fallbackTitle,
            recording.ArtistCredit?.FirstOrDefault()?.Name ?? fallbackArtist,
            recording.Isrcs?.FirstOrDefault() ?? fallbackIsrc,
            recording.Id,
            recording.Length);
    }

    private async Task<MusicBrainzRecordingDto?> LookupByIsrcAsync(
        string isrc,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<MusicBrainzIsrcLookupResponse>(
            $"/ws/2/isrc/{Uri.EscapeDataString(isrc)}?fmt=json&inc=artist-credits+isrcs",
            cancellationToken);

        return response?.Recordings?.FirstOrDefault(recording =>
                   recording.Isrcs?.Any(value => string.Equals(value, isrc, StringComparison.OrdinalIgnoreCase)) == true)
               ?? response?.Recordings?.FirstOrDefault();
    }

    private async Task<MusicBrainzRecordingDto?> SearchByNamesAsync(
        string trackName,
        string artist,
        string? albumName,
        CancellationToken cancellationToken)
    {
        var clauses = new List<string>
        {
            $"recording:\"{trackName}\"",
            $"artist:\"{artist}\""
        };
        if (!string.IsNullOrWhiteSpace(albumName))
        {
            clauses.Add($"release:\"{albumName}\"");
        }

        var query = Uri.EscapeDataString(string.Join(" AND ", clauses));
        var response = await httpClient.GetFromJsonAsync<MusicBrainzRecordingSearchResponse>(
            $"/ws/2/recording?fmt=json&limit=5&query={query}&inc=artist-credits+isrcs+releases",
            cancellationToken);

        return response?.Recordings?
            .OrderByDescending(recording => MusicMetadataLookupMatch.TitleAndArtistMatch(
                trackName,
                artist,
                recording.Title,
                recording.ArtistCredit?.FirstOrDefault()?.Name))
            .ThenByDescending(recording => int.TryParse(recording.Score, out var score) ? score : 0)
            .FirstOrDefault();
    }

    public static void ConfigureHttpClient(
        HttpClient httpClient,
        MusicBrainzOptions options)
    {
        httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    }

    private sealed class MusicBrainzIsrcLookupResponse
    {
        [JsonPropertyName("recordings")]
        public List<MusicBrainzRecordingDto>? Recordings { get; init; }
    }

    private sealed class MusicBrainzRecordingSearchResponse
    {
        [JsonPropertyName("recordings")]
        public List<MusicBrainzRecordingDto>? Recordings { get; init; }
    }

    private sealed class MusicBrainzRecordingDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("length")]
        public int? Length { get; init; }

        [JsonPropertyName("score")]
        public string? Score { get; init; }

        [JsonPropertyName("isrcs")]
        public List<string>? Isrcs { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<MusicBrainzArtistCreditDto>? ArtistCredit { get; init; }
    }

    private sealed class MusicBrainzArtistCreditDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
