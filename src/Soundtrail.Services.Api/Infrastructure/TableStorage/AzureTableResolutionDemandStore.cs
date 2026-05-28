using System.Collections.Concurrent;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Api.Infrastructure.TableStorage;

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
