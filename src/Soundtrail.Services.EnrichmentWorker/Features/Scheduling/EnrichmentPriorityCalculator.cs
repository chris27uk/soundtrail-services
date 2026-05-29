using Soundtrail.Services.EnrichmentWorker.Configuration;
using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Models;
using Microsoft.Extensions.Options;

namespace Soundtrail.Services.EnrichmentWorker.Scheduling;

public sealed class EnrichmentPriorityCalculator(IOptions<EnrichmentWorkerOptions> options)
{
    private readonly PriorityWeightsOptions weights = options.Value.Priority;

    public int Calculate(ResolutionDemand demand, EnrichmentStage stage)
    {
        var providerPenalty = stage switch
        {
            EnrichmentStage.MusicBrainzApi => 10,
            EnrichmentStage.AppleMusic => 50,
            EnrichmentStage.ITunesSearch => 75,
            _ => 0
        };

        var score =
            demand.DemandCount * weights.DemandCountWeight +
            demand.DistinctInstallCount * weights.DistinctInstallCountWeight +
            demand.DistinctIpHashCount * weights.RecentDemandCountWeight +
            (demand.BestKnownIsrc is not null ? weights.KnownIsrcBonus : 0) +
            (demand.BestKnownMbid is not null ? weights.KnownMbidBonus : 0) +
            (demand.HasStrongMetadata ? weights.KnownArtistAndTitleBonus : 0) +
            (demand.HighestTrustLevelSeen > 0 ? weights.AttestedDemandBonus : 0) -
            providerPenalty -
            (demand.IsSuspicious ? weights.HighRiskPenalty : 0) -
            (demand.PreviousFailureCount * weights.PreviousFailurePenalty);

        return score;
    }
}
