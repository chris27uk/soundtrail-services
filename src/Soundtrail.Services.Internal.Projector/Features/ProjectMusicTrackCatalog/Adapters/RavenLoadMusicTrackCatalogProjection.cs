using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.ProjectMusicTrackCatalog.Adapters;

public sealed class RavenLoadMusicTrackCatalogProjection(
    IAsyncDocumentSession session,
    RavenMusicTrackCatalogProjectionMapper mapper) : ILoadMusicTrackCatalogProjectionPort
{
    public async Task<MusicTrackCatalogProjection> LoadAsync(
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var track = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        CatalogArtistRecordDto? artist = null;
        if (!string.IsNullOrWhiteSpace(track?.ArtistId))
        {
            artist = await session.LoadAsync<CatalogArtistRecordDto>(
                CatalogArtistRecordDto.GetDocumentId(track.ArtistId),
                cancellationToken);
        }

        CatalogAlbumRecordDto? album = null;
        if (!string.IsNullOrWhiteSpace(track?.AlbumId))
        {
            album = await session.LoadAsync<CatalogAlbumRecordDto>(
                CatalogAlbumRecordDto.GetDocumentId(track.AlbumId),
                cancellationToken);
        }

        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(
            CatalogProjectionCheckpointDocument.GetDocumentId(musicCatalogId.Value),
            cancellationToken);

        return mapper.ToDomain(musicCatalogId, track, artist, album, checkpoint);
    }
}
