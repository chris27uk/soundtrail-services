using Raven.Client.Documents.Session;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

public sealed class RavenSaveMusicTrackCatalogProjection(
    IAsyncDocumentSession session,
    RavenMusicTrackCatalogProjectionMapper mapper) : ISaveMusicTrackCatalogProjectionPort
{
    public async Task SaveAsync(
        MusicTrackCatalogProjection projection,
        CancellationToken cancellationToken)
    {
        var trackDocumentId = CatalogTrackRecordDto.GetDocumentId(projection.MusicCatalogId.Value);
        var trackDocument = await session.LoadAsync<CatalogTrackRecordDto>(trackDocumentId, cancellationToken)
            ?? new CatalogTrackRecordDto
            {
                Id = trackDocumentId
            };

        mapper.MapOntoTrackDocument(trackDocument, projection);
        await session.StoreAsync(trackDocument, cancellationToken);

        if (projection.Artist is not null && !string.IsNullOrWhiteSpace(projection.Artist.ArtistId))
        {
            var artistDocumentId = CatalogArtistRecordDto.GetDocumentId(projection.Artist.ArtistId);
            var artistDocument = await session.LoadAsync<CatalogArtistRecordDto>(artistDocumentId, cancellationToken)
                ?? new CatalogArtistRecordDto
                {
                    Id = artistDocumentId
                };

            mapper.MapOntoArtistDocument(artistDocument, projection.Artist);
            await session.StoreAsync(artistDocument, cancellationToken);
        }

        if (projection.Album is not null && !string.IsNullOrWhiteSpace(projection.Album.AlbumId))
        {
            var albumDocumentId = CatalogAlbumRecordDto.GetDocumentId(projection.Album.AlbumId);
            var albumDocument = await session.LoadAsync<CatalogAlbumRecordDto>(albumDocumentId, cancellationToken)
                ?? new CatalogAlbumRecordDto
                {
                    Id = albumDocumentId
                };

            mapper.MapOntoAlbumDocument(albumDocument, projection.Album);
            await session.StoreAsync(albumDocument, cancellationToken);
        }

        var checkpointDocumentId = CatalogProjectionCheckpointDocument.GetDocumentId(projection.MusicCatalogId.Value);
        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(checkpointDocumentId, cancellationToken)
            ?? new CatalogProjectionCheckpointDocument
            {
                Id = checkpointDocumentId,
                MusicCatalogId = projection.MusicCatalogId.Value
            };
        checkpoint.LastAppliedVersion = projection.ProjectionVersion;
        checkpoint.UpdatedAt = projection.Track.UpdatedAt;
        await session.StoreAsync(checkpoint, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
