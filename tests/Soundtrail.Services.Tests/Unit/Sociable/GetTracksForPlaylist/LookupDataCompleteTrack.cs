using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed record LookupDataCompleteTrack(
    TrackReference PlaylistTrack,
    CatalogDiscoveryEntry CatalogEntry,
    IReadOnlyDictionary<ProviderName, Uri> StreamingLocations)
{
    public static LookupDataCompleteTrack MatchingCatalogTrack(
        string playlistArtist,
        string playlistTitle,
        string catalogArtist,
        string catalogTitle,
        string albumTitle,
        DateOnly releaseDate,
        string? releaseType,
        int durationMs,
        DateTimeOffset catalogUpdatedAt,
        params (ProviderName Provider, string Url)[] streamingLocations)
    {
        var trackId = TrackId.TryCreate(catalogArtist, catalogTitle, albumTitle, releaseDate, releaseType) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unsupported track id creation result.")
        };
        var track = new Track(trackId)
        {
            Title = catalogTitle,
            ArtistName = catalogArtist,
            AlbumTitle = albumTitle,
            DurationMs = durationMs,
            ReleaseDate = releaseDate,
            ReleaseType = releaseType,
            UpdatedAt = catalogUpdatedAt
        };

        return new LookupDataCompleteTrack(
            new TrackReference(ArtistName.From(playlistArtist), playlistTitle),
            new CatalogDiscoveryEntry(
                ArtistId.From($"musicbrainz-artist:{StringNormalizationExtensions.Normalize(catalogArtist)}"),
                new CatalogItem.MusicTrack(track)),
            streamingLocations.ToDictionary(
                static location => location.Provider,
                static location => new Uri(location.Url)));
    }
}
