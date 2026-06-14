using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record ListTracksByAlbumCommand(ArtistId ArtistId, AlbumId AlbumId);
