using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Adapters;

public sealed class MusicBrainzGetTrackMetadata(HttpClient httpClient) : IGetTrackMetadata
{
    public async Task<SongMetadata?> GetMetadataAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var recording = await searchCriteria.Match(
            async query => await SearchByQueryAsync(query, cancellationToken),
            async (track, artist, album) => await SearchByNamesAsync(
                track,
                artist,
                album,
                cancellationToken),
            async isrc => await LookupByIsrcAsync(isrc, cancellationToken));

        if (recording is null)
        {
            return null;
        }

        var fallbackTitle = searchCriteria.Match(query => query, (track, _, _) => track, __ => string.Empty);
        var fallbackArtist = searchCriteria.Match(_ => string.Empty, (_, artist, _) => artist, __ => string.Empty);
        var fallbackIsrc = searchCriteria.Match<string?>(_ => null, (_, _, _) => null, isrc => isrc);

        return new SongMetadata(
            recording.Title ?? fallbackTitle,
            recording.ArtistCredit?.FirstOrDefault()?.Name ?? fallbackArtist,
            recording.Isrcs?.FirstOrDefault() ?? fallbackIsrc,
            recording.Id,
            recording.Length,
            recording.Releases?.FirstOrDefault(static release => !string.IsNullOrWhiteSpace(release.Title))?.Title,
            ParseReleaseDate(recording.Releases),
            recording.ArtistCredit?.FirstOrDefault()?.Artist?.Id,
            recording.Releases?.FirstOrDefault(static release => !string.IsNullOrWhiteSpace(release.Id))?.Id);
    }

    private async Task<MusicBrainzRecordingDto?> LookupByIsrcAsync(
        string isrc,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<MusicBrainzIsrcLookupResponse>(
            $"/ws/2/isrc/{Uri.EscapeDataString(isrc)}?fmt=json&inc=artist-credits+isrcs+releases",
            cancellationToken);

        var exactMatches = response?.Recordings?
            .Where(recording => recording.Isrcs?.Any(value => string.Equals(value, isrc, StringComparison.OrdinalIgnoreCase)) == true)
            .ToArray()
            ?? [];

        if (exactMatches.Length == 0)
        {
            return response?.Recordings?.FirstOrDefault();
        }

        return HasAmbiguousIdentifiers(exactMatches)
            ? null
            : exactMatches[0];
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

        return SelectBestNameMatch(response?.Recordings, trackName, artist, albumName);
    }

    private async Task<MusicBrainzRecordingDto?> SearchByQueryAsync(
        string queryText,
        CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString(queryText);
        var response = await httpClient.GetFromJsonAsync<MusicBrainzRecordingSearchResponse>(
            $"/ws/2/recording?fmt=json&limit=5&query={query}&inc=artist-credits+isrcs+releases",
            cancellationToken);

        return SelectBestQueryMatch(response?.Recordings);
    }

    private static MusicBrainzRecordingDto? SelectBestNameMatch(
        IReadOnlyList<MusicBrainzRecordingDto>? recordings,
        string trackName,
        string artist,
        string? albumName)
    {
        var ranked = (recordings ?? [])
            .Select(recording => new RankedRecording(recording, Score(recording, trackName, artist, albumName)))
            .OrderByDescending(item => item.Score)
            .ToArray();

        if (ranked.Length == 0 || ranked[0].Score < 100)
        {
            return null;
        }

        if (ranked.Length > 1 && ranked[0].Score - ranked[1].Score < 10)
        {
            return null;
        }

        return ranked[0].Recording;
    }

    private static MusicBrainzRecordingDto? SelectBestQueryMatch(
        IReadOnlyList<MusicBrainzRecordingDto>? recordings)
    {
        var ranked = (recordings ?? [])
            .Select(recording => new RankedRecording(recording, Score(recording)))
            .OrderByDescending(item => item.Score)
            .ToArray();

        if (ranked.Length == 0 || ranked[0].Score < 90)
        {
            return null;
        }

        if (ranked.Length > 1 && ranked[0].Score - ranked[1].Score < 10)
        {
            return null;
        }

        return ranked[0].Recording;
    }

    private static int Score(
        MusicBrainzRecordingDto recording,
        string expectedTitle,
        string expectedArtist,
        string? expectedAlbum)
    {
        var score = 0;
        if (MusicMetadataLookupMatch.TitleAndArtistMatch(
                expectedTitle,
                expectedArtist,
                recording.Title,
                recording.ArtistCredit?.FirstOrDefault()?.Name))
        {
            score += 100;
        }

        var normalizedAlbum = MusicMetadataLookupMatch.Normalize(expectedAlbum);
        if (!string.IsNullOrWhiteSpace(normalizedAlbum)
            && recording.Releases?.Any(release =>
                string.Equals(
                    MusicMetadataLookupMatch.Normalize(release.Title),
                    normalizedAlbum,
                    StringComparison.Ordinal)) == true)
        {
            score += 20;
        }

        if (recording.Releases?.Any(static release => !string.IsNullOrWhiteSpace(release.Date)) == true)
        {
            score += 2;
        }

        score += int.TryParse(recording.Score, out var musicBrainzScore) ? musicBrainzScore / 10 : 0;
        return score;
    }

    private static int Score(MusicBrainzRecordingDto recording)
    {
        var score = int.TryParse(recording.Score, out var musicBrainzScore) ? musicBrainzScore / 10 : 0;

        if (recording.Releases?.Any(static release => !string.IsNullOrWhiteSpace(release.Date)) == true)
        {
            score += 2;
        }

        return score;
    }

    private static bool HasAmbiguousIdentifiers(IReadOnlyList<MusicBrainzRecordingDto> exactMatches) =>
        exactMatches
            .Select(recording => recording.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Skip(1)
            .Any();

    private static DateOnly? ParseReleaseDate(IReadOnlyList<MusicBrainzReleaseDto>? releases)
    {
        var releaseDate = releases?
            .Select(static release => release.Date)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

        return DateOnly.TryParse(releaseDate, out var parsedDate)
            ? parsedDate
            : null;
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

        [JsonPropertyName("releases")]
        public List<MusicBrainzReleaseDto>? Releases { get; init; }
    }

    private sealed class MusicBrainzArtistCreditDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("artist")]
        public MusicBrainzArtistDto? Artist { get; init; }
    }

    private sealed class MusicBrainzReleaseDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }
    }

    private sealed class MusicBrainzArtistDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }
    }

    private sealed record RankedRecording(MusicBrainzRecordingDto Recording, int Score);
}
