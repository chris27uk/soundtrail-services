using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class InMemoryUpsertDiscoveryStatus : IUpsertDiscoveryStatusPort
{
    private readonly Dictionary<string, DiscoveryStatusUpdate> updates = [];

    public IReadOnlyDictionary<string, DiscoveryStatusUpdate> Updates => updates;

    public Task UpsertAsync(
        DiscoveryStatusUpdate update,
        CancellationToken cancellationToken)
    {
        updates[update.QueryKey.Value] = update;
        return Task.CompletedTask;
    }
}
