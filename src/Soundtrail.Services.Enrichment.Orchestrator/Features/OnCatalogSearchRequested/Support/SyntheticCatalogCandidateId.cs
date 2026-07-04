using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;

public static class SyntheticCatalogCandidateId
{
    public static MusicCatalogId ForSearch(LookupCriteria searchCriteria) =>
        MusicCatalogId.From(
            $"discovery_{DiscoveryQueryKey.StableValueFor(searchCriteria).Replace(':', '_')}");
}
