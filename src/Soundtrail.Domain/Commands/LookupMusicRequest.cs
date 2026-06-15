using Soundtrail.Domain.Discovery;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Commands;

public sealed record LookupMusicRequest(
    DiscoveryQueryKey QueryKey,
    NormalizedSearchQuery Query,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset OccurredAt,
    CorrelationId CorrelationId);
