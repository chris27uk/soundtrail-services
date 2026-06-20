using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record VersionedCatalogSearchDiscoveryEvent(
    int Version,
    IDomainEvent Event);
