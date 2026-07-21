using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;

public sealed class MusicbrainzCatalogBrowsePort(
    HttpClient httpClient,
    IOptions<MusicBrainzOptions> options)
    : IReadAlbumsByArtistIdPort, IReadTracksByArtistIdPort, IReadTracksByAlbumIdPort
{
    public const string HttpClientName = "MusicbrainzCatalogBrowse";
    private const int DefaultLimit = 100;

    private readonly MusicBrainzOptions options = options.Value;

    public async Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(
                "/ws/2/release",
                new Dictionary<string, string?>
                {
                    ["artist"] = artistId.Value,
                    ["fmt"] = "json",
                    ["limit"] = DefaultLimit.ToString()
                }),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReleaseBrowseResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz release browse response body is required.");

        if (payload.Releases is null)
        {
            throw new InvalidOperationException("MusicBrainz release browse response must include releases.");
        }

        return payload.Releases
            .Where(release => !string.IsNullOrWhiteSpace(release.Id) && !string.IsNullOrWhiteSpace(release.Title))
            .Select(release => new CatalogDiscoveryEntry(
                artistId,
                new Domain.Catalog.CatalogItem.MusicAlbum(
                    new Album(
                        AlbumId.From(artistId.Value, release.Id!),
                        release.Title,
                        release.Id,
                        ParseDate(release.Date),
                        artworkUrl: null,
                        updatedAt: DateTimeOffset.UtcNow))))
            .ToArray();
    }

    async Task<IReadOnlyList<CatalogDiscoveryEntry>> IReadTracksByArtistIdPort.ReadAsync(
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(
                "/ws/2/recording",
                new Dictionary<string, string?>
                {
                    ["artist"] = artistId.Value,
                    ["fmt"] = "json",
                    ["limit"] = DefaultLimit.ToString()
                }),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RecordingBrowseResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz recording browse response body is required.");

        if (payload.Recordings is null)
        {
            throw new InvalidOperationException("MusicBrainz recording browse response must include recordings.");
        }

        return payload.Recordings
            .Where(recording => !string.IsNullOrWhiteSpace(recording.Id) && !string.IsNullOrWhiteSpace(recording.Title))
            .Select(recording =>
            {
                var artistName = recording.ArtistCredit?.FirstOrDefault()?.Name ?? string.Empty;
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
                    new Domain.Catalog.CatalogItem.MusicTrack(track));
            })
            .ToArray();
    }

    async Task<IReadOnlyList<CatalogDiscoveryEntry>> IReadTracksByAlbumIdPort.ReadAsync(
        AlbumId albumId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            QueryHelpers.AddQueryString(
                $"/ws/2/release/{albumId.ArtistAlbumId}",
                new Dictionary<string, string?>
                {
                    ["inc"] = "recordings+artist-credits+isrcs",
                    ["fmt"] = "json"
                }),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ReleaseLookupResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MusicBrainz release lookup response body is required.");

        if (payload.Media is null)
        {
            throw new InvalidOperationException("MusicBrainz release lookup response must include media.");
        }

        var artistId = ArtistId.From(albumId.ArtistId);
        var releaseDate = ParseDate(payload.Date);
        var albumTitle = payload.Title;

        return payload.Media
            .SelectMany(media => media.Tracks ?? [])
            .Where(track => !string.IsNullOrWhiteSpace(track.Title))
            .Select(track =>
            {
                var artistName = track.ArtistCredit?.FirstOrDefault()?.Name
                    ?? payload.ArtistCredit?.FirstOrDefault()?.Name
                    ?? string.Empty;
                var trackId = TrackId.Create(artistName, track.Title!, albumTitle, releaseDate);
                var discoveredTrack = new Track(trackId)
                {
                    Title = track.Title!,
                    ArtistName = artistName,
                    AlbumId = albumId.StableValue,
                    AlbumTitle = albumTitle,
                    DurationMs = track.Length,
                    Isrc = track.Recording?.Isrcs?.FirstOrDefault(),
                    Mbid = track.Recording?.Id,
                    ReleaseDate = releaseDate,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                return new CatalogDiscoveryEntry(
                    artistId,
                    new Domain.Catalog.CatalogItem.MusicTrack(discoveredTrack));
            })
            .ToArray();
    }

    private static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParse(value, out var date) ? date : null;
    }

    private sealed class ReleaseBrowseResponse
    {
        public List<ReleaseResult>? Releases { get; init; }
    }

    private sealed class RecordingBrowseResponse
    {
        public List<RecordingResult>? Recordings { get; init; }
    }

    private sealed class ReleaseLookupResponse
    {
        public string? Title { get; init; }
        public string? Date { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<ArtistCreditResult>? ArtistCredit { get; init; }

        public List<MediaResult>? Media { get; init; }
    }

    private sealed class ReleaseResult
    {
        public string? Id { get; init; }
        public string? Title { get; init; }
        public string? Date { get; init; }
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

    private sealed class MediaResult
    {
        public List<ReleaseTrackResult>? Tracks { get; init; }
    }

    private sealed class ReleaseTrackResult
    {
        public string? Title { get; init; }
        public int? Length { get; init; }

        [JsonPropertyName("artist-credit")]
        public List<ArtistCreditResult>? ArtistCredit { get; init; }

        public RecordingReferenceResult? Recording { get; init; }
    }

    private sealed class RecordingReferenceResult
    {
        public string? Id { get; init; }
        public List<string>? Isrcs { get; init; }
    }

    private sealed class ArtistCreditResult
    {
        public string? Name { get; init; }
    }
}
