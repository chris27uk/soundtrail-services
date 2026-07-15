using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Domain.Catalog;

public sealed record CatalogTrackHierarchy(ArtistId? ArtistId, AlbumId? AlbumId);
