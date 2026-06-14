using Soundtrail.Domain.Commands;

namespace Soundtrail.Domain.Search;

public sealed class SearchCatalogHandler(
    ICatalogSearchPort catalogSearch,
    IQueueLookupMusicRequestPort queueLookupMusicRequest) : IHandler<SearchCatalogCommand, SearchCatalogResponse>
{
    public async Task<SearchCatalogResponse> Handle(
        SearchCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var local = await catalogSearch.SearchAsync(command, cancellationToken);
        var discovery = local.Discovery;

        if (!local.IsComplete && discovery is null)
        {
            await queueLookupMusicRequest.EnqueueAsync(command.Query.ToNewLookupRequest(), cancellationToken);
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
