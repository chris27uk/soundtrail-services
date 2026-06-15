using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public static class Candidates
{
    public static PotentialCatalogLookupWork ExistingCandidate(
        MusicCatalogId musicCatalogId,
        int requestCount = 2,
        int highestTrustLevelSeen = 0,
        int riskScore = 5,
        PotentialCatalogLookupWorkStatus status = PotentialCatalogLookupWorkStatus.Pending,
        DateTimeOffset? nextEligibleAt = null) =>
        new(
            MusicCatalogId: musicCatalogId,
            RequestCount: requestCount,
            HighestTrustLevelSeen: highestTrustLevelSeen,
            RiskScore: riskScore,
            Status: status,
            NextEligibleAt: nextEligibleAt);

    public static PotentialCatalogLookupWork PopularEligibleCandidate(string musicCatalogId = "mc_track_high") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 3,
            highestTrustLevelSeen: 2,
            riskScore: 10);

    public static PotentialCatalogLookupWork LowDemandEligibleCandidate(string musicCatalogId = "mc_track_low") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 1,
            highestTrustLevelSeen: 1,
            riskScore: 10);

    public static PotentialCatalogLookupWork MediumRiskCandidate(string musicCatalogId = "mc_track_medium") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 5,
            highestTrustLevelSeen: 3,
            riskScore: 30);

    public static PotentialCatalogLookupWork HighTrustLowDemandCandidate(string musicCatalogId = "mc_track_trusted") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 1,
            highestTrustLevelSeen: 2,
            riskScore: 10);

    public static PotentialCatalogLookupWork HighRiskCandidate(string musicCatalogId = "mc_track_high_risk") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 60);

    public static PotentialCatalogLookupWork ResolvedCandidate(string musicCatalogId = "mc_track_resolved") =>
        ExistingCandidate(
            MusicCatalogId.From(musicCatalogId),
            requestCount: 2,
            highestTrustLevelSeen: 1,
            riskScore: 10,
            status: PotentialCatalogLookupWorkStatus.Resolved);

    public static PotentialCatalogLookupWork EligibleCandidate(string musicCatalogId = "mc_track_1") =>
        ExistingCandidate(MusicCatalogId.From(musicCatalogId));

    public static PotentialCatalogLookupWork NotYetEligibleCandidate(MusicCatalogId musicCatalogId) =>
        ExistingCandidate(
            musicCatalogId,
            requestCount: 1,
            highestTrustLevelSeen: 0,
            riskScore: 0,
            status: PotentialCatalogLookupWorkStatus.Pending,
            nextEligibleAt: new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));
}
