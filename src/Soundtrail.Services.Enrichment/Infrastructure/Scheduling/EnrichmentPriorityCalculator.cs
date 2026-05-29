using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Configuration;

namespace Soundtrail.Services.Enrichment.Infrastructure.Scheduling;

public sealed class EnrichmentPriorityCalculator(EnrichmentWorkerOptions options)
{
    private readonly PriorityWeightsOptions weights = options.Priority;

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
            demand.DemandCount * this.weights.DemandCountWeight +
            demand.DistinctInstallCount * this.weights.DistinctInstallCountWeight +
            demand.DistinctIpHashCount * this.weights.RecentDemandCountWeight +
            (demand.BestKnownIsrc is not null ? this.weights.KnownIsrcBonus : 0) +
            (demand.BestKnownMbid is not null ? this.weights.KnownMbidBonus : 0) +
            (demand.HasStrongMetadata ? this.weights.KnownArtistAndTitleBonus : 0) +
            (demand.HighestTrustLevelSeen > 0 ? this.weights.AttestedDemandBonus : 0) -
            providerPenalty -
            (demand.IsSuspicious ? this.weights.HighRiskPenalty : 0) -
            (demand.PreviousFailureCount * this.weights.PreviousFailurePenalty);

        return score;
    }
}
