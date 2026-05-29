using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Providers;

public interface IEnrichmentProvider
{
    EnrichmentStage Stage { get; }

    ProviderName Provider { get; }

    Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
