using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Discovery;

public sealed record MusicTrackSearchStarted(
    MusicSearchCriteria SearchCriteria,
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAt,
    CorrelationId CorrelationId) : IDomainEvent;
