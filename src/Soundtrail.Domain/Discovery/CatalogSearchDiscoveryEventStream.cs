using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery;

public sealed record CatalogSearchDiscoveryEventStream(
    int Version,
    IReadOnlyList<IDomainEvent> Events);
