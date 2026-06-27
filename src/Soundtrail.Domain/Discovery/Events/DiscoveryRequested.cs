using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Events;

public sealed record DiscoveryRequested(
    MusicSearchCriteria SearchCriteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
