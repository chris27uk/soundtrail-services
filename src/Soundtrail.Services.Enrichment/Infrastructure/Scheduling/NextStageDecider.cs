using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Infrastructure.Scheduling;

public sealed class NextStageDecider
{
    public EnrichmentStage? Decide(ResolutionDemand demand)
    {
        if (!demand.AttemptedStages.Contains(EnrichmentStage.LocalMapping))
        {
            return EnrichmentStage.LocalMapping;
        }

        if (!demand.AttemptedStages.Contains(EnrichmentStage.LocalMusicBrainzDataset))
        {
            return EnrichmentStage.LocalMusicBrainzDataset;
        }

        if (!demand.AttemptedStages.Contains(EnrichmentStage.MusicBrainzApi))
        {
            return EnrichmentStage.MusicBrainzApi;
        }

        if (demand.BestKnownIsrc is not null &&
            !demand.AttemptedStages.Contains(EnrichmentStage.AppleMusic))
        {
            return EnrichmentStage.AppleMusic;
        }

        if (demand.HasStrongMetadata &&
            !demand.AttemptedStages.Contains(EnrichmentStage.ITunesSearch))
        {
            return EnrichmentStage.ITunesSearch;
        }

        return null;
    }
}
