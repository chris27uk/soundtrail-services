using Raven.Client.Documents.Session;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class RavenSaveMusicTrackCatalogProjection(
    IAsyncDocumentSession session,
    ITypeTranslator translator) : ISaveMusicTrackCatalogProjectionPort
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

        translator.MapOnto(projection.Track, trackDocument);
        await session.StoreAsync(trackDocument, cancellationToken);

        if (projection.Artist is not null && !string.IsNullOrWhiteSpace(projection.Artist.ArtistId))
        {
            var artistDocumentId = CatalogArtistRecordDto.GetDocumentId(projection.Artist.ArtistId);
            var artistDocument = await session.LoadAsync<CatalogArtistRecordDto>(artistDocumentId, cancellationToken)
                ?? new CatalogArtistRecordDto
                {
                    Id = artistDocumentId
                };

            translator.MapOnto(projection.Artist, artistDocument);
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

            translator.MapOnto(projection.Album, albumDocument);
            await session.StoreAsync(albumDocument, cancellationToken);
        }

        var checkpointDocumentId = CatalogProjectionCheckpointDocument.GetDocumentId(projection.MusicCatalogId.Value);
        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(checkpointDocumentId, cancellationToken)
            ?? new CatalogProjectionCheckpointDocument
            {
                Id = checkpointDocumentId,
            };
        translator.MapOnto(projection, checkpoint);
        await session.StoreAsync(checkpoint, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
