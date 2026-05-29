using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;

namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public interface IEnrichmentProvider
{
    EnrichmentStage Stage { get; }

    ProviderName Provider { get; }

    Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
