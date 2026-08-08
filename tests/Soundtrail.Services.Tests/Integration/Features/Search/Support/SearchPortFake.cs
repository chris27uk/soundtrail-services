using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Integration.Features.Search.Support;

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

    public void Seed(SearchResponse? searchResponse) => this.response = searchResponse;

    public Task<SearchResponse?> SearchAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
    {
        RequestedCriteria.Add(searchCriteria);
        return this.resolver is null
            ? Task.FromResult(this.response)
            : this.resolver(searchCriteria, cancellationToken);
    }
}
