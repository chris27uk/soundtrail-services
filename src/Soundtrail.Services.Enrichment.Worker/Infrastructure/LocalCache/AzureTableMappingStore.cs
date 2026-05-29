using Soundtrail.Services.Enrichment.Features.LocalCache;
using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using System.Collections.Concurrent;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.LocalCache;

public sealed class AzureTableMappingStore : IMappingStorePort
{
    private readonly ConcurrentDictionary<string, TrackMapping> mappings = new();

    public Task<TrackMapping?> FindAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        TrackMapping? mapping = null;

        if (demand.BestKnownIsrc is not null)
        {
            mappings.TryGetValue($"isrc:{demand.BestKnownIsrc.Value}", out mapping);
        }

        if (mapping is null && demand.BestKnownMbid is not null)
        {
            mappings.TryGetValue($"mbid:{demand.BestKnownMbid.Value}", out mapping);
        }

        return Task.FromResult(mapping);
    }

    public Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken)
    {
        if (mapping.Isrc is not null)
        {
            mappings[$"isrc:{mapping.Isrc.Value}"] = mapping;
        }

        if (mapping.Mbid is not null)
        {
            mappings[$"mbid:{mapping.Mbid.Value}"] = mapping;
        }

        if (mapping.AppleMusicId is not null)
        {
            mappings[$"apple:{mapping.AppleMusicId.Value}"] = mapping;
        }

        return Task.CompletedTask;
    }
}
