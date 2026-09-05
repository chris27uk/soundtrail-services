using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;

namespace Soundtrail.Adapters.CatalogProjection;

public static class ArtistCatalogProjectionSnapshotMapper
{
    public static ArtistCatalogProjectionSnapshotDto ToDto(ArtistCatalogProjection projection, int streamVersion) =>
        new()
        {
            Id = ArtistCatalogProjectionSnapshotDto.GetDocumentId(projection.ArtistId.Value),
            ArtistId = projection.ArtistId.Value,
            StreamVersion = streamVersion,
            ArtistName = projection.ArtistName,
            ArtworkUrl = projection.ArtworkUrl,
            MusicBrainzArtistId = projection.MusicBrainzArtistId,
            UpdatedAt = projection.UpdatedAt,
            Albums = projection.Albums.Select(static album => new ArtistCatalogProjectionSnapshotAlbumDto
            {
                AlbumId = album.AlbumId.StableValue,
                AlbumTitle = album.AlbumTitle,
                SourceAlbumId = album.SourceAlbumId,
                ReleaseDate = album.ReleaseDate,
                ArtworkUrl = album.ArtworkUrl
            }).ToArray(),
            Tracks = projection.Tracks.Select(static track => new ArtistCatalogProjectionSnapshotTrackDto
            {
                TrackId = track.TrackId.Value,
                Title = track.Title,
                ArtistName = track.ArtistName,
                AlbumId = track.AlbumId,
                AlbumTitle = track.AlbumTitle,
                DurationMs = track.DurationMs,
                Isrc = track.Isrc,
                ReleaseDate = track.ReleaseDate,
                ReleaseType = track.ReleaseType,
                ArtworkUrl = track.ArtworkUrl,
                StreamingLocations = track.StreamingLocations.Select(static location =>
                    new ArtistCatalogProjectionSnapshotStreamingLocationDto
                    {
                        Provider = location.Provider.Value,
                        ExternalId = location.ExternalId,
                        Url = location.Url
                    }).ToArray()
            }).ToArray()
        };

    public static ArtistCatalogProjection ToProjection(ArtistCatalogProjectionSnapshotDto dto) =>
        new(
            ArtistId.From(dto.ArtistId),
            dto.ArtistName,
            dto.ArtworkUrl,
            dto.MusicBrainzArtistId,
            dto.UpdatedAt,
            dto.Albums.Select(static album => new ArtistCatalogAlbumProjection(
                AlbumId.From(album.AlbumId),
                album.AlbumTitle,
                album.SourceAlbumId,
                album.ReleaseDate,
                album.ArtworkUrl)).ToArray(),
            dto.Tracks.Select(static track => new ArtistCatalogTrackProjection(
                TrackId.From(track.TrackId),
                track.Title,
                track.ArtistName,
                track.AlbumId,
                track.AlbumTitle,
                track.DurationMs,
                track.Isrc,
                track.ReleaseDate,
                track.ReleaseType,
                track.ArtworkUrl,
                track.StreamingLocations.Select(static location =>
                    new ArtistCatalogStreamingLocationProjection(
                        ProviderName.From(location.Provider),
                        location.ExternalId,
                        location.Url)).ToArray())).ToArray());
}
