namespace Soundtrail.Services.Enrichment.Infrastructure.Orchestration;

public interface IEnrichmentAttemptStorePort
{
    Task RecordAsync(
        EnrichmentAttempt attempt,
        CancellationToken cancellationToken);
}
