using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Integration.Search.Api.Ports;

internal sealed class SearchPortFake : ISearchPort
{
    private readonly Func<SearchCriteria, CancellationToken, Task<SearchResponse?>>? resolver;
    private SearchResponse? response;

    public SearchPortFake(SearchResponse? response = null) => this.response = response;

    private SearchPortFake(Func<SearchCriteria, CancellationToken, Task<SearchResponse?>> resolver) =>
        this.resolver = resolver;

    public List<SearchCriteria> RequestedCriteria { get; } = [];

    public static SearchPortFake Create(
        Func<SearchCriteria, CancellationToken, Task<SearchResponse?>> resolver) =>
        new(resolver);

    public void Seed(SearchResponse? searchResponse) => response = searchResponse;

    public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        RequestedCriteria.Add(searchCriteria);
        return resolver is null
            ? Task.FromResult(response)
            : resolver(searchCriteria, cancellationToken);
    }
}
