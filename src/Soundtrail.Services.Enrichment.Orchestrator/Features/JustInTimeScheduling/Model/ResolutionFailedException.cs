using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.JustInTimeScheduling.Model
{
    public class ResolutionFailedException(MusicCatalogResolutionOutcome outcome)
        : Exception($"Music catalog resolution failed with outcome '{outcome}'.")
    {
        public MusicCatalogResolutionOutcome Outcome { get; } = outcome;
    }
}
