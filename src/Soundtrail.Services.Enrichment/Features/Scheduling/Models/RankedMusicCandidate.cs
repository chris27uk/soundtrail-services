using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Models;

public sealed record RankedMusicCandidate(
    QueryId RankedMusicCandidateId,
    MusicCatalogId MusicCatalogId,
    NormalizedSearchQuery Query,
    int RequestCount,
    int HighestTrustLevelSeen,
    int RiskScore,
    RankedMusicCandidateStatus Status,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? NextEligibleAt)
{
    public bool IsSuspicious => RiskScore >= 100;

    public bool IsPending => Status == RankedMusicCandidateStatus.Pending;

    public bool IsEligibleAt(DateTimeOffset when) =>
        NextEligibleAt is null || NextEligibleAt <= when;

    public static RankedMusicCandidate Create(LookupMusicRequest request, MusicCatalogId musicCatalogId)
    {
        return new RankedMusicCandidate(
            RankedMusicCandidateId: QueryId.New(),
            MusicCatalogId: musicCatalogId,
            Query: request.Query,
            RequestCount: 1,
            HighestTrustLevelSeen: request.TrustLevel,
            RiskScore: request.RiskScore,
            Status: RankedMusicCandidateStatus.Pending,
            FirstSeenAt: request.OccurredAt,
            LastSeenAt: request.OccurredAt,
            NextEligibleAt: null);
    }

    public RankedMusicCandidate Register(LookupMusicRequest request)
    {
        return this with
        {
            RequestCount = RequestCount + 1,
            HighestTrustLevelSeen = Math.Max(HighestTrustLevelSeen, request.TrustLevel),
            RiskScore = Math.Max(RiskScore, request.RiskScore),
            LastSeenAt = request.OccurredAt
        };
    }
}
