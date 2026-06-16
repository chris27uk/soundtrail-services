using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogSearchDiscoveryEventStream(
    int Version,
    IReadOnlyList<IDomainEvent> Events);
