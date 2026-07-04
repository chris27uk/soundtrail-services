using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record CatalogDiscoveryWorkRequested(
    EnrichmentQuery Query,
    LookupPriorityBand Priority,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt) : IDomainEvent;
