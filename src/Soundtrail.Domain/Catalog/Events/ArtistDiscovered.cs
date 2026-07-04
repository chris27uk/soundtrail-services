namespace Soundtrail.Domain.Catalog.Events;

public sealed record ArtistDiscovered(Artist Artist, DateTimeOffset ObservedAt) : IMusicTrackEvent;
