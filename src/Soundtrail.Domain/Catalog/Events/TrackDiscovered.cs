using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Domain.Catalog.Events;

public sealed record TrackDiscovered(
    Track Track,
    CatalogTrackHierarchy Hierarchy,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
