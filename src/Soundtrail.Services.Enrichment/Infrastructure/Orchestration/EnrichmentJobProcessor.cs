using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Shared.Configuration;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public sealed class EnrichmentJobProcessor(
    IDemandStorePort demandStore,
    IProviderCircuitStatePort circuitState,
    IProviderConcurrencyPort providerConcurrency,
    IProviderBudgetPort providerBudgets,
    IEnrichmentAttemptStorePort attemptStore,
    IMappingStorePort mappingStore,
    IQueryCachePort queryCache,
    ISearchIndexPort searchIndex,
    IEnumerable<IEnrichmentProvider> providers,
    EnrichmentWorkerOptions options,
    IClockPort clock)
{
    public async Task<EnrichmentJobResult> ProcessAsync(
        EnrichmentJob job,
        CancellationToken cancellationToken)
    {
        var demand = await demandStore.GetAsync(job.QueryId, cancellationToken);
        if (demand is null || demand.Status == ResolutionDemandStatus.Resolved)
        {
            return new EnrichmentJobResult(EnrichmentOutcome.Rejected);
        }

        var providerState = await circuitState.GetAsync(job.Provider, cancellationToken);
        if (providerState.State == CircuitState.Open)
        {
            return new EnrichmentJobResult(EnrichmentOutcome.ProviderUnavailable);
        }

        ConcurrencyLease? lease = null;

        if (job.Provider != ProviderName.Local)
        {
            lease = await providerConcurrency.TryAcquireAsync(job.Provider, cancellationToken);
            if (lease is null)
            {
                return new EnrichmentJobResult(EnrichmentOutcome.RetryLater);
            }
        }

        var policy = options.PolicyFor(job.Provider);
        var budget = await providerBudgets.TryConsumeAsync(job.Provider, policy, cancellationToken);
        if (!budget.Allowed)
        {
            if (lease is not null)
            {
                await providerConcurrency.ReleaseAsync(lease, cancellationToken);
            }

            return new EnrichmentJobResult(EnrichmentOutcome.RetryLater);
        }

        var provider = providers.Single(candidate => candidate.Stage == job.Stage);
        var startedAt = clock.UtcNow;
        EnrichmentJobResult result;

        try
        {
            result = await provider.EnrichAsync(demand, cancellationToken);
        }
        finally
        {
            if (lease is not null)
            {
                await providerConcurrency.ReleaseAsync(lease, cancellationToken);
            }
        }

        var finishedAt = clock.UtcNow;

        await attemptStore.RecordAsync(
            new EnrichmentAttempt(
                AttemptId: $"{job.JobId}:{job.Attempts + 1}",
                job.QueryId,
                job.Stage,
                job.Provider,
                job.Attempts + 1,
                result.Outcome,
                ErrorCode: null,
                ProviderStatusCode: null,
                startedAt,
                finishedAt,
                result.RetryAt),
            cancellationToken);

        if (result.Mapping is not null)
        {
            await mappingStore.UpsertAsync(result.Mapping, cancellationToken);
            await searchIndex.UpsertAsync(result.Mapping, cancellationToken);
            await queryCache.RefreshAsync(demand, result.Mapping, cancellationToken);
        }

        if (result.Outcome == EnrichmentOutcome.Resolved)
        {
            await demandStore.MarkResolvedAsync(job.QueryId, cancellationToken);
        }
        else if (result.UpdatedDemand is not null)
        {
            await demandStore.UpsertAsync(result.UpdatedDemand, cancellationToken);
        }

        return result;
    }
}
