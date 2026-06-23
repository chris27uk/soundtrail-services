using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryRequested(
    CatalogSearchCriteria Criteria,
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
