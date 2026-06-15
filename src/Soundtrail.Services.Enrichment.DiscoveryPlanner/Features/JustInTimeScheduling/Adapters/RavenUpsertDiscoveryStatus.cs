using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class RavenUpsertDiscoveryStatus(
    IDocumentStore documentStore) : IUpsertDiscoveryStatusPort
{
    public async Task UpsertAsync(
        DiscoveryStatusUpdate update,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        await session.StoreAsync(new DiscoveryStatusRecordDto
        {
            Id = DiscoveryStatusRecordDto.GetDocumentId(update.QueryKey.Value),
            QueryKey = update.QueryKey.Value,
            Status = update.Status.ToString(),
            Priority = update.Priority?.ToString() ?? string.Empty,
            WillBeLookedUp = update.WillBeLookedUp,
            EstimatedRetryAfterSeconds = update.EstimatedRetryAfterSeconds,
            EarliestExpectedCompletionAt = update.EarliestExpectedCompletionAt,
            Reason = update.Reason,
            UpdatedAt = update.UpdatedAt
        }, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }
}
