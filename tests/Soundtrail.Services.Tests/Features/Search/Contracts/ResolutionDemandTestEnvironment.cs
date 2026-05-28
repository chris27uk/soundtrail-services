using Soundtrail.Services.Api.Infrastructure.TableStorage;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Features.Search.Contracts;

internal sealed class ResolutionDemandTestEnvironment
{
    private ResolutionDemandTestEnvironment(IResolutionDemandPort store)
    {
        Store = store;
    }

    public IResolutionDemandPort Store { get; }

    public static ResolutionDemandTestEnvironment Create(StorageMode mode) =>
        mode switch
        {
            StorageMode.Fake => new(new FakeResolutionDemandPort()),
            StorageMode.AzureTable => new(new AzureTableResolutionDemandStore()),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}

internal sealed class FakeResolutionDemandPort : IResolutionDemandPort
{
    private readonly Dictionary<string, QueryId> _queryIds = new();

    public Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        if (!_queryIds.TryGetValue(query.Value, out var queryId))
        {
            queryId = QueryId.New();
            _queryIds.Add(query.Value, queryId);
        }

        return Task.FromResult(queryId);
    }
}
