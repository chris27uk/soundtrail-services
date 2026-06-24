using Soundtrail.Domain.Discovery;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record CatalogSearchAttempt(
    CatalogSearchCriteria Criteria,
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
