using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public static class Candidates
{
    public static RankedMusicCandidate ExistingCandidate(
        MusicCatalogId musicCatalogId,
        int requestCount = 2,
        int highestTrustLevelSeen = 0,
        int riskScore = 5,
        RankedMusicCandidateStatus status = RankedMusicCandidateStatus.Pending,
        DateTimeOffset? nextEligibleAt = null) =>
        new(
            MusicCatalogId: musicCatalogId,
            RequestCount: requestCount,
            HighestTrustLevelSeen: highestTrustLevelSeen,
            RiskScore: riskScore,
            Status: status,
            NextEligibleAt: nextEligibleAt);

    public static RankedMusicCandidate PopularEligibleCandidate(string musicCatalogId = "mc_track_high") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 3,
            highestTrustLevelSeen: 2,
            riskScore: 10);

    public static RankedMusicCandidate LowDemandEligibleCandidate(string musicCatalogId = "mc_track_low") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10);

    public static RankedMusicCandidate MediumRiskCandidate(string musicCatalogId = "mc_track_medium") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 5,
            highestTrustLevelSeen: 3,
            riskScore: 30);

    public static RankedMusicCandidate HighTrustLowDemandCandidate(string musicCatalogId = "mc_track_trusted") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 1,
            highestTrustLevelSeen: 2,
            riskScore: 10);

    public static RankedMusicCandidate HighRiskCandidate(string musicCatalogId = "mc_track_high_risk") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 60);

    public static RankedMusicCandidate ResolvedCandidate(string musicCatalogId = "mc_track_resolved") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: RankedMusicCandidateStatus.Resolved);

    public static RankedMusicCandidate EligibleCandidate(string musicCatalogId = "mc_track_1") =>
        ExistingCandidate(MusicCatalogId.From(musicCatalogId));

    public static RankedMusicCandidate NotYetEligibleCandidate(MusicCatalogId musicCatalogId) =>
        ExistingCandidate(
            musicCatalogId,
            requestCount: 1,
            highestTrustLevelSeen: 0,
            riskScore: 0,
            status: RankedMusicCandidateStatus.Pending,
            nextEligibleAt: new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));
}
