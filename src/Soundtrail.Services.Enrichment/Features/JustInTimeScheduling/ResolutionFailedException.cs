using Soundtrail.Services.Enrichment.Shared.Search.Resolution;

namespace Soundtrail.Services.Enrichment.Features.JustInTimeScheduling
{
    public class ResolutionFailedException(MusicCatalogResolutionOutcome outcome)
        : Exception($"Music catalog resolution failed with outcome '{outcome}'.")
    {
        public MusicCatalogResolutionOutcome Outcome { get; } = outcome;
    }
}
