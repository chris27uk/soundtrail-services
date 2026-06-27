namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record GetTrackCommand(ArtistId ArtistId, AlbumId AlbumId, TrackId TrackId);
