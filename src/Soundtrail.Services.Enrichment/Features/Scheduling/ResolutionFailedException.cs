using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling
{
    public class ResolutionFailedException : Exception
    {
        public ResolutionFailedException(MusicCatalogResolutionOutcome outcome)
            : base($"Music catalog resolution failed with outcome '{outcome}'.")
        {
            Outcome = outcome;
        }

        public MusicCatalogResolutionOutcome Outcome { get; }
    }
}
