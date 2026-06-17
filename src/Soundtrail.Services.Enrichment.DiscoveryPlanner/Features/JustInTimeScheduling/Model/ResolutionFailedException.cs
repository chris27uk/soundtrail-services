using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Model
{
    public class ResolutionFailedException(MusicCatalogResolutionOutcome outcome)
        : Exception($"Music catalog resolution failed with outcome '{outcome}'.")
    {
        public MusicCatalogResolutionOutcome Outcome { get; } = outcome;
    }
}
