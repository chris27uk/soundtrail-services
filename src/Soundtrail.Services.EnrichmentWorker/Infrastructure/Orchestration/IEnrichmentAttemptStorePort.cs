using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface IEnrichmentAttemptStorePort
{
    Task RecordAsync(
        EnrichmentAttempt attempt,
        CancellationToken cancellationToken);
}
