using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery;

public sealed record VersionedCatalogSearchDiscoveryEvent(
    int Version,
    IDomainEvent Event);
