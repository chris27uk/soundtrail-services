using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

internal static class RavenMappings
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
}
