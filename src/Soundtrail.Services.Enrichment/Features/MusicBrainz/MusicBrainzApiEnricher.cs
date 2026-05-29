using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.MusicBrainz;

public sealed class MusicBrainzApiEnricher(IMusicBrainzClient client) : IEnrichmentProvider
{
    public EnrichmentStage Stage => EnrichmentStage.MusicBrainzApi;

    public ProviderName Provider => ProviderName.MusicBrainz;

    public async Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        var mapping = await client.TryResolveAsync(demand, cancellationToken);
        return mapping is null
            ? new EnrichmentJobResult(EnrichmentOutcome.NotFound)
            : new EnrichmentJobResult(EnrichmentOutcome.PartiallyResolved, mapping);
    }
}
