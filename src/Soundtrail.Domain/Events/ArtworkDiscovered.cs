using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Events;

public sealed record ArtworkDiscovered(
    CatalogEntityKind EntityKind,
    string? EntityId,
    Uri Url,
    string Source,
    DateTimeOffset ObservedAt) : IMusicTrackEvent;
