using System.Collections.Concurrent;
using MusicResolver.Application.Ports;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Infrastructure.TableStorage;

public sealed class AzureTableResolutionDemandStore : IResolutionDemandPort
{
    private readonly ConcurrentDictionary<string, QueryId> _queryIds = new();

    public Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        var queryId = _queryIds.GetOrAdd(query.Value, _ => QueryId.New());
        return Task.FromResult(queryId);
    }
}
