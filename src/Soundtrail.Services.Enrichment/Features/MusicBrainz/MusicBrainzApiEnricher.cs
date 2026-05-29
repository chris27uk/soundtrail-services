using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Providers;

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
