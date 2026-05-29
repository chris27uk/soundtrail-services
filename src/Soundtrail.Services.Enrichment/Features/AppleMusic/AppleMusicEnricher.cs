using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.AppleMusic;

public sealed class AppleMusicEnricher(IAppleMusicClient client) : IEnrichmentProvider
{
    public EnrichmentStage Stage => EnrichmentStage.AppleMusic;

    public ProviderName Provider => ProviderName.AppleMusic;

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
