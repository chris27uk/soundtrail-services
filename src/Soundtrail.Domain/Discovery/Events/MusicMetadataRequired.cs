using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public sealed record TrackMetadataLookupRequested(
    MusicSearchCriteria SearchCriteria,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequiredAt,
    CorrelationId CorrelationId) : IDomainEvent;
