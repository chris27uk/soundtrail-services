using Raven.Client.Documents.Indexes;
using Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters.Documents;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.BacklogScheduling.Adapters.Indexes;

internal sealed class PotentialCatalogLookupWork_ByPlanning : AbstractIndexCreationTask<RavenPotentialCatalogLookupWorkRecordDto>
{
    public PotentialCatalogLookupWork_ByPlanning()
    {
        Map = candidates => from candidate in candidates
                            select new
                            {
                                candidate.Status,
                                candidate.NextEligibleAt,
                                candidate.HighestTrustLevelSeen,
                                candidate.RequestCount,
                                candidate.RiskScore
                            };
    }
}
