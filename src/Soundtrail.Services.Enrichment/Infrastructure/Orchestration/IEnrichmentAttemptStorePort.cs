using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface IEnrichmentAttemptStorePort
{
    Task RecordAsync(
        EnrichmentAttempt attempt,
        CancellationToken cancellationToken);
}
