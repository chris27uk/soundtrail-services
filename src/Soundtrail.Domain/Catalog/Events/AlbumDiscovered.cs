using Soundtrail.Domain.Catalog.Albums;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record AlbumDiscovered(Album Album, DateTimeOffset ObservedAt) : IMusicTrackEvent;
