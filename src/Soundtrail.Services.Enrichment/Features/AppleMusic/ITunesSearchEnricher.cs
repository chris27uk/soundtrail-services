using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.AppleMusic;

public sealed class ITunesSearchEnricher(IITunesSearchClient client) : IEnrichmentProvider
{
    public EnrichmentStage Stage => EnrichmentStage.ITunesSearch;

    public ProviderName Provider => ProviderName.ITunesSearch;

    public async Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        var mapping = await client.TryResolveAsync(demand, cancellationToken);
        return mapping is null
            ? new EnrichmentJobResult(EnrichmentOutcome.NotFound)
            : new EnrichmentJobResult(EnrichmentOutcome.Resolved, mapping);
    }
}
