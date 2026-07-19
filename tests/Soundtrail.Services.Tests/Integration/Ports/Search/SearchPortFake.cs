using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.Search;

internal sealed class SearchPortFake(SearchResponse? response = null) : ISearchPort
{
    public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken) => Task.FromResult(response);
}
