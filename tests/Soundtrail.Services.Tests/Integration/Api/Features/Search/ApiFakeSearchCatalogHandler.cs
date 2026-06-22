using Soundtrail.Contracts;
using Soundtrail.Domain;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Api.Features.Search;

public sealed class ApiFakeSearchCatalogHandler : IApiHandler<SearchCatalogCommand, SearchCatalogResponse>
{
    private readonly List<SearchCatalogCommand> requests = [];
    private SearchCatalogResponse response = new(
        "default",
        [],
        new SearchDiscovery(false, null, null));

    public IReadOnlyList<SearchCatalogCommand> Requests => requests;

    public void ClearRequests() => requests.Clear();

    public void RespondWith(SearchCatalogResponse response) => this.response = response;

    public Task<SearchCatalogResponse> Handle(
        SearchCatalogCommand request,
        CancellationToken cancellationToken = default)
    {
        requests.Add(request);
        return Task.FromResult(response);
    }
}
