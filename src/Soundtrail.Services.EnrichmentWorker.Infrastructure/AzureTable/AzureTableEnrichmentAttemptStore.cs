using System.Collections.Concurrent;
using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;

namespace Soundtrail.Services.EnrichmentWorker.Infrastructure.AzureTable;

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
