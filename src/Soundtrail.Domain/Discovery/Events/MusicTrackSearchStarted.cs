using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Discovery;

public sealed record MusicTrackSearchStarted(
    CatalogSearchCriteria Criteria,
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAt,
    CorrelationId CorrelationId) : IDomainEvent;
