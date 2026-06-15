using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog;

public sealed class SearchCatalogHandler(
    ICatalogSearchPort catalogSearch,
    IRequestDiscoveryPort requestDiscoveryPort) : IHandler<SearchCatalogCommand, SearchCatalogResponse>
{
    public async Task<SearchCatalogResponse> Handle(
        SearchCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var local = await catalogSearch.SearchAsync(command, cancellationToken);
        var discovery = local.Discovery;

        if (!local.IsComplete && discovery is null)
        {
            var queryKey = command.ToDiscoveryQueryKey();
            await requestDiscoveryPort.TryRequestAsync(
                new RequestDiscoveryCommand(
                    queryKey,
                    command.Query.ToNewLookupRequest(queryKey)),
                cancellationToken);
            discovery = new SearchDiscovery(
                WillBeLookedUp: true,
                Reason: "Local results incomplete",
                RetryAfterSeconds: null);
        }

        return new SearchCatalogResponse(
            command.Query.Value,
            local.Results,
            discovery ?? new SearchDiscovery(false, null, null));
    }
}
