using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record MusicMetadataRequired(
    MusicSeekOrSearchCriteria Criteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequiredAt,
    CorrelationId CorrelationId) : IDomainEvent;
