using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Configuration;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Infrastructure.Scheduling;

public sealed class EnrichmentScheduler(
    IDemandStorePort demandStore,
    IEnrichmentQueuePort queue,
    IProviderConcurrencyPort providerConcurrency,
    IProviderBudgetPort providerBudgets,
    IProviderCircuitStatePort circuitState,
    EnrichmentCandidateSelector candidateSelector,
    EnrichmentWorkerOptions options,
    IClockPort clock)
{
    public async Task<IReadOnlyList<EnrichmentJob>> RunAsync(CancellationToken cancellationToken)
    {
        if (!options.Scheduler.Enabled)
        {
            return Array.Empty<EnrichmentJob>();
        }

        var now = clock.UtcNow;
        var unresolved = await demandStore.GetUnresolvedAsync(now, cancellationToken);
        var candidates = candidateSelector.Select(unresolved, now)
            .Take(options.Scheduler.MaxJobsPerRun)
            .ToArray();

        var jobs = new List<EnrichmentJob>();

        foreach (var candidate in candidates)
        {
            var provider = ToProvider(candidate.Stage);
            var policy = options.PolicyFor(provider);

            if (!policy.Enabled || candidate.PriorityScore < policy.MinimumPriorityScore)
            {
                continue;
            }

            var providerState = await circuitState.GetAsync(provider, cancellationToken);
            if (providerState.State == CircuitState.Open)
            {
                continue;
            }

            if (provider != ProviderName.Local &&
                !await providerConcurrency.IsAvailableAsync(provider, cancellationToken))
            {
                continue;
            }

            if (!await providerBudgets.IsAvailableAsync(provider, policy, cancellationToken))
            {
                continue;
            }

            var job = new EnrichmentJob(
                Guid.NewGuid().ToString("N"),
                candidate.Demand.QueryId,
                candidate.Demand.NormalizedQuery,
                candidate.Stage,
                provider,
                candidate.PriorityScore,
                Attempts: 0,
                NotBefore: now,
                CreatedAt: now,
                CorrelationId: Guid.NewGuid().ToString("N"));

            await queue.EnqueueAsync(job, cancellationToken);
            jobs.Add(job);
        }

        return jobs;
    }

    private static ProviderName ToProvider(EnrichmentStage stage) =>
        stage switch
        {
            EnrichmentStage.LocalMapping => ProviderName.Local,
            EnrichmentStage.LocalMusicBrainzDataset => ProviderName.Local,
            EnrichmentStage.MusicBrainzApi => ProviderName.MusicBrainz,
            EnrichmentStage.AppleMusic => ProviderName.AppleMusic,
            EnrichmentStage.ITunesSearch => ProviderName.ITunesSearch,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };
}
