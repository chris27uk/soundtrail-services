using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.Search;

internal sealed class SearchPortFake(SearchResponse? response = null) : ISearchPort
{
    public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken) => Task.FromResult(response);
}
