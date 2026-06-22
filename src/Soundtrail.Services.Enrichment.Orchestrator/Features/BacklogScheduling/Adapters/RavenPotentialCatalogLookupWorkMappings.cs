using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters;

internal static class RavenPotentialCatalogLookupWorkMappings
{
    public static PotentialCatalogLookupWork ToDomain(this RavenPotentialCatalogLookupWorkRecordDto document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<PotentialCatalogLookupWorkStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt);

    public static RavenPotentialCatalogLookupWorkRecordDto ToRecordDto(this PotentialCatalogLookupWork candidate) =>
        new()
        {
            Id = RavenPotentialCatalogLookupWorkRecordDto.GetDocumentId(candidate.MusicCatalogId.Value),
            MusicCatalogId = candidate.MusicCatalogId.Value,
            RequestCount = candidate.RequestCount,
            HighestTrustLevelSeen = candidate.HighestTrustLevelSeen,
            RiskScore = candidate.RiskScore,
            Status = candidate.Status.ToString(),
            NextEligibleAt = candidate.NextEligibleAt
        };

    public static void ApplyTo(this PotentialCatalogLookupWork candidate, RavenPotentialCatalogLookupWorkRecordDto document)
    {
        document.Id = RavenPotentialCatalogLookupWorkRecordDto.GetDocumentId(candidate.MusicCatalogId.Value);
        document.MusicCatalogId = candidate.MusicCatalogId.Value;
        document.RequestCount = candidate.RequestCount;
        document.HighestTrustLevelSeen = candidate.HighestTrustLevelSeen;
        document.RiskScore = candidate.RiskScore;
        document.Status = candidate.Status.ToString();
        document.NextEligibleAt = candidate.NextEligibleAt;
    }
}
