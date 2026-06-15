using Raven.Client.Documents;
using Soundtrail.Contracts;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class RavenUpsertCatalogSearchStatus(
    IDocumentStore documentStore) : IUpsertCatalogSearchStatusPort
{
    public async Task UpsertAsync(
        CatalogSearchStatusUpdate update,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        await session.StoreAsync(new CatalogSearchStatusRecordDto
        {
            Id = CatalogSearchStatusRecordDto.GetDocumentId(update.Criteria.Value),
            Criteria = update.Criteria.Value,
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
