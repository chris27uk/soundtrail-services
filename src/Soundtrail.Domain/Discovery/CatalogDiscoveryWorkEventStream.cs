using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogDiscoveryWorkEventStream(
    int Version,
    IReadOnlyList<IDomainEvent> Events);
