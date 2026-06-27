using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkEventStream(
    int Version,
    IReadOnlyList<IDomainEvent> Events);
