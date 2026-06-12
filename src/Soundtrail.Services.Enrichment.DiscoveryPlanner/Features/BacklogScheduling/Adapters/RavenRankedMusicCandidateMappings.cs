using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;

internal static class RavenRankedMusicCandidateMappings
{
    public static RankedMusicCandidate ToDomain(this RavenRankedMusicCandidateDocument document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<RankedMusicCandidateStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt);

    public static RavenRankedMusicCandidateDocument ToDocument(this RankedMusicCandidate candidate) =>
        new()
        {
            Id = RavenRankedMusicCandidateDocument.GetDocumentId(candidate.MusicCatalogId.Value),
            MusicCatalogId = candidate.MusicCatalogId.Value,
            RequestCount = candidate.RequestCount,
            HighestTrustLevelSeen = candidate.HighestTrustLevelSeen,
            RiskScore = candidate.RiskScore,
            Status = candidate.Status.ToString(),
            NextEligibleAt = candidate.NextEligibleAt
        };

    public static void ApplyTo(this RankedMusicCandidate candidate, RavenRankedMusicCandidateDocument document)
    {
        document.Id = RavenRankedMusicCandidateDocument.GetDocumentId(candidate.MusicCatalogId.Value);
        document.MusicCatalogId = candidate.MusicCatalogId.Value;
        document.RequestCount = candidate.RequestCount;
        document.HighestTrustLevelSeen = candidate.HighestTrustLevelSeen;
        document.RiskScore = candidate.RiskScore;
        document.Status = candidate.Status.ToString();
        document.NextEligibleAt = candidate.NextEligibleAt;
    }
}
