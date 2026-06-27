namespace Soundtrail.Domain.Catalog.Events;

public sealed record ArtworkDiscovered(
    CatalogEntityKind EntityKind,
    string? EntityId,
    Uri Url,
    string Source,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
