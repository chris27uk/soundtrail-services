using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class InMemoryUpsertCatalogSearchStatus : IUpsertCatalogSearchStatusPort
{
    private readonly Dictionary<string, CatalogSearchStatusUpdate> updates = [];

    public IReadOnlyDictionary<string, CatalogSearchStatusUpdate> Updates => updates;

    public Task UpsertAsync(
        CatalogSearchStatusUpdate update,
        CancellationToken cancellationToken)
    {
        updates[update.Criteria.Value] = update;
        return Task.CompletedTask;
    }
}
