namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record ListTracksByAlbumCommand(ArtistId ArtistId, AlbumId AlbumId);
