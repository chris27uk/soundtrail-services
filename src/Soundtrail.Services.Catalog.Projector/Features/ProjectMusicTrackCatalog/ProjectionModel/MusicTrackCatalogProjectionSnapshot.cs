using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.ProjectionModel;

public sealed record MusicTrackCatalogProjectionSnapshot(
    MusicCatalogId MusicCatalogId,
    CatalogTrackProjection Track,
    CatalogArtistProjection? Artist,
    CatalogAlbumProjection? Album,
    int ProjectionVersion);
