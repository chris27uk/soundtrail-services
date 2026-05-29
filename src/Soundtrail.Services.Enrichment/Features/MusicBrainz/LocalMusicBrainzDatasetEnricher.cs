using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Providers;

public sealed class LocalMusicBrainzDatasetEnricher(IMusicBrainzDatasetReader datasetReader) : IEnrichmentProvider
{
    public EnrichmentStage Stage => EnrichmentStage.LocalMusicBrainzDataset;

    public ProviderName Provider => ProviderName.Local;

    public async Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        var mapping = await datasetReader.TryResolveAsync(demand, cancellationToken);
        return mapping is null
            ? new EnrichmentJobResult(EnrichmentOutcome.NotFound)
            : new EnrichmentJobResult(EnrichmentOutcome.PartiallyResolved, mapping);
    }
}
