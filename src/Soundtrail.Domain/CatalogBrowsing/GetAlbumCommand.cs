using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record GetAlbumCommand(ArtistId ArtistId, AlbumId AlbumId);
