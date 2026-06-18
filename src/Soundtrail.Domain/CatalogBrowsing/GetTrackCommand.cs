using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record GetTrackCommand(ArtistId ArtistId, AlbumId AlbumId, TrackId TrackId);
