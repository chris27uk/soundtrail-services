namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record GetAlbumCommand(ArtistId ArtistId, AlbumId AlbumId);
