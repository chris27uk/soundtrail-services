using Soundtrail.Services.Api.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Support;

public sealed record CatalogProjectionDocuments(
    CatalogTrackRecordDto Track,
    CatalogArtistRecordDto? Artist,
    CatalogAlbumRecordDto? Album);
