using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Catalog.Projection;

public sealed record VersionedCatalogEvent(
    int Version,
    IDomainEvent Event);
