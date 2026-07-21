using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;

public sealed class MusicbrainzCatalogSearchPort(
    HttpClient httpClient,
    IOptions<MusicBrainzOptions> options) : IReadCatalogEntriesBySearchCriteriaPort
{
    public const string HttpClientName = "MusicbrainzCatalogSearch";
    private const int DefaultLimit = 10;

    private readonly MusicBrainzOptions options = options.Value;

    public async Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        SearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        var results = new List<CatalogDiscoveryEntry>();

        if (Includes(searchCriteria.SearchTypes, SearchType.Artist))
        {
            results.AddRange(await ReadArtistsAsync(searchCriteria.Query, cancellationToken));
        }

        if (Includes(searchCriteria.SearchTypes, SearchType.Album))
        {
            results.AddRange(await ReadAlbumsAsync(searchCriteria.Query, cancellationToken));
        }

        if (Includes(searchCriteria.SearchTypes, SearchType.Track))
        {
            results.AddRange(await ReadTracksAsync(searchCriteria.Query, cancellationToken));
        }

        return results
            .GroupBy(entry => StableKey(entry.Item))
            .Select(group => group.First())
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadArtistsAsync(string query, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BuildSearchUri("/ws/2/artist", query), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ArtistSearchResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz artist search response body is required.");

        if (payload.Artists is null)
        {
            throw new InvalidOperationException("MusicBrainz artist search response must include artists.");
        }

        return payload.Artists
            .Where(artist => !string.IsNullOrWhiteSpace(artist.Id) && !string.IsNullOrWhiteSpace(artist.Name))
            .Select(artist =>
            {
                var artistId = ArtistId.From(artist.Id!);
                return new CatalogDiscoveryEntry(
                    artistId,
                    new CatalogItem.MusicArtist(
                        new Artist
                        {
                            Id = artistId,
                            Name = ArtistName.From(artist.Name)
                        }));
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAlbumsAsync(string query, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BuildSearchUri("/ws/2/release", query), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReleaseSearchResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz release search response body is required.");

        if (payload.Releases is null)
        {
            throw new InvalidOperationException("MusicBrainz release search response must include releases.");
        }

        return payload.Releases
            .Where(release => !string.IsNullOrWhiteSpace(release.Id) && !string.IsNullOrWhiteSpace(release.Title))
            .Select(release =>
            {
                var artistName = release.ArtistCredit?.FirstOrDefault()?.Name ?? string.Empty;
                var artistId = ArtistId.From(release.ArtistCredit?.FirstOrDefault()?.Artist?.Id ?? FallbackArtistId(artistName));
                var albumId = AlbumId.From(artistId.Value, release.Id!);

                return new CatalogDiscoveryEntry(
                    artistId,
                    new CatalogItem.MusicAlbum(
                        new Album(
                            albumId,
                            release.Title,
                            release.Id,
                            ParseDate(release.Date),
                            artworkUrl: null,
                            updatedAt: DateTimeOffset.UtcNow)));
            })
            .ToArray();
    }

    private async Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadTracksAsync(string query, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(BuildSearchUri("/ws/2/recording", query), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RecordingSearchResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz recording search response body is required.");

        if (payload.Recordings is null)
        {
            throw new InvalidOperationException("MusicBrainz recording search response must include recordings.");
        }

        return payload.Recordings
            .Where(recording => !string.IsNullOrWhiteSpace(recording.Id) && !string.IsNullOrWhiteSpace(recording.Title))
            .Select(recording =>
            {
                var credit = recording.ArtistCredit?.FirstOrDefault();
                var artistName = credit?.Name ?? string.Empty;
                var artistId = ArtistId.From(credit?.Artist?.Id ?? FallbackArtistId(artistName));
                var releaseDate = ParseDate(recording.FirstReleaseDate);
                var trackId = TrackId.Create(artistName, recording.Title!, releaseDate: releaseDate);
                var track = new Track(trackId)
                {
                    Title = recording.Title!,
                    ArtistName = artistName,
                    DurationMs = recording.Length,
                    Isrc = recording.Isrcs?.FirstOrDefault(),
                    Mbid = recording.Id,
                    ReleaseDate = releaseDate,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                return new CatalogDiscoveryEntry(
                    artistId,
                    new CatalogItem.MusicTrack(track));
            })
            .ToArray();
    }

    private string BuildSearchUri(string path, string query) =>
        QueryHelpers.AddQueryString(
            path,
            new Dictionary<string, string?>
            {
                ["query"] = query,
                ["fmt"] = "json",
                ["limit"] = DefaultLimit.ToString()
            });

    private static bool Includes(SearchType value, SearchType flag) => (value & flag) == flag;

    private static string StableKey(CatalogItem item) =>
        item switch
        {
            CatalogItem.MusicArtist(var artist) => $"artist:{artist.Id.Value}",
            CatalogItem.MusicAlbum(var album) => $"album:{album.AlbumId.StableValue}",
            CatalogItem.MusicTrack(var track) => $"track:{track.TrackId.Value}",
            CatalogItem.MusicPlaylist => "playlist",
            _ => throw new InvalidOperationException($"Unsupported catalog item '{item.GetType().Name}'.")
        };

    private static string FallbackArtistId(string artistName) =>
        "musicbrainz-artist:" + MusicIdentityText.NormalizeCompact(artistName);

    private static DateOnly? ParseDate(string? value)
    {
        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }

        return null;
    }

    private sealed class ArtistSearchResponse
    {
        public List<ArtistResult>? Artists { get; init; }
    }

    private sealed class ReleaseSearchResponse
    {
        public List<ReleaseResult>? Releases { get; init; }
    }

    private sealed class RecordingSearchResponse
    {
        public List<RecordingResult>? Recordings { get; init; }
    }

    private sealed class ArtistResult
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
    }

    private sealed class ReleaseResult
    {
        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Date { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<ArtistCreditResult>? ArtistCredit { get; init; }
    }

    private sealed class RecordingResult
    {
        public string? Id { get; init; }
        public string? Title { get; init; }
        public int? Length { get; init; }

        [JsonPropertyName("first-release-date")]
        public string? FirstReleaseDate { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<ArtistCreditResult>? ArtistCredit { get; init; }

        public List<string>? Isrcs { get; init; }
    }

    private sealed class ArtistCreditResult
    {
        public string? Name { get; init; }
        public ArtistReferenceResult? Artist { get; init; }
    }

    private sealed class ArtistReferenceResult
    {
        public string? Id { get; init; }
    }
}
