using Soundtrail.Services.Enrichment.Budgets;
using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Configuration;

public sealed class EnrichmentWorkerOptions
{
    public SchedulerOptions Scheduler { get; init; } = new();

    public PriorityWeightsOptions Priority { get; init; } = new();

    public ProviderRateLimitPolicy MusicBrainz { get; init; } = ProviderRateLimitPolicy.For(ProviderName.MusicBrainz);

    public ProviderRateLimitPolicy AppleMusic { get; init; } = ProviderRateLimitPolicy.For(ProviderName.AppleMusic);

    public ProviderRateLimitPolicy ITunesSearch { get; init; } = ProviderRateLimitPolicy.For(ProviderName.ITunesSearch);

    public ProviderRateLimitPolicy PolicyFor(ProviderName provider) =>
        provider switch
        {
            ProviderName.MusicBrainz => MusicBrainz,
            ProviderName.AppleMusic => AppleMusic,
            ProviderName.ITunesSearch => ITunesSearch,
            _ => ProviderRateLimitPolicy.For(provider)
        };
}

public sealed class SchedulerOptions
{
    public bool Enabled { get; init; } = true;

    public int IntervalSeconds { get; init; } = 60;

    public int MaxJobsPerRun { get; init; } = 100;
}

public sealed class PriorityWeightsOptions
{
    public int DemandCountWeight { get; init; } = 5;

    public int DistinctInstallCountWeight { get; init; } = 10;

    public int RecentDemandCountWeight { get; init; } = 3;

    public int KnownIsrcBonus { get; init; } = 50;

    public int KnownMbidBonus { get; init; } = 30;

    public int KnownArtistAndTitleBonus { get; init; } = 20;

    public int AttestedDemandBonus { get; init; } = 20;

    public int HighRiskPenalty { get; init; } = 100;

    public int PreviousFailurePenalty { get; init; } = 10;
}
