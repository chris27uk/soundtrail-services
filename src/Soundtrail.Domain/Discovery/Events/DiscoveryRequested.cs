using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record DiscoveryRequested(
    MusicSearchCriteria SearchCriteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt,
    CorrelationId CorrelationId) : IDomainEvent;
