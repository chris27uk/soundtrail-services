using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Providers;

public interface IEnrichmentProvider
{
    EnrichmentStage Stage { get; }

    ProviderName Provider { get; }

    Task<EnrichmentJobResult> EnrichAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken);
}
