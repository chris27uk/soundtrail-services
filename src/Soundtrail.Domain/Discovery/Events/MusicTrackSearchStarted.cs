using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery.Events;

public sealed record CatalogSearchCandidateRecorded(
    MusicSearchCriteria SearchCriteria,
    MusicCatalogId MusicCatalogId,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset StartedAt,
    CorrelationId CorrelationId) : IDomainEvent;
