using System.Collections.Concurrent;
using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Orchestration;

public sealed class AzureTableEnrichmentAttemptStore : IEnrichmentAttemptStorePort
{
    private readonly ConcurrentBag<EnrichmentAttempt> attempts = new();

    public Task RecordAsync(
        EnrichmentAttempt attempt,
        CancellationToken cancellationToken)
    {
        attempts.Add(attempt);
        return Task.CompletedTask;
    }
}
