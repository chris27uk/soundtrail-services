using Soundtrail.Services.Enrichment.Infrastructure.Orchestration;
using System.Collections.Concurrent;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Orchestration;

public sealed class AzureTableDemandStore : IDemandStorePort
{
    private readonly ConcurrentDictionary<string, ResolutionDemand> demandById = new();

    public Task<IReadOnlyList<ResolutionDemand>> GetUnresolvedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var unresolved = demandById.Values
            .Where(demand => demand.Status is ResolutionDemandStatus.Unresolved or ResolutionDemandStatus.PartiallyResolved)
            .ToArray();

        return Task.FromResult<IReadOnlyList<ResolutionDemand>>(unresolved);
    }

    public Task<ResolutionDemand?> GetAsync(
        QueryId queryId,
        CancellationToken cancellationToken)
    {
        demandById.TryGetValue(queryId.Value, out var demand);
        return Task.FromResult(demand);
    }

    public Task UpsertAsync(
        ResolutionDemand demand,
        CancellationToken cancellationToken)
    {
        demandById[demand.QueryId.Value] = demand;
        return Task.CompletedTask;
    }

    public Task MarkResolvedAsync(
        QueryId queryId,
        CancellationToken cancellationToken)
    {
        if (demandById.TryGetValue(queryId.Value, out var demand))
        {
            demandById[queryId.Value] = demand with { Status = ResolutionDemandStatus.Resolved };
        }

        return Task.CompletedTask;
    }
}
