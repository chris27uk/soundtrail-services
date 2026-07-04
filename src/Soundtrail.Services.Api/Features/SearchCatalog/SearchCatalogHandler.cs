using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;

namespace Soundtrail.Services.Api.Features.SearchCatalog;

public sealed class SearchCatalogHandler(
    ICatalogSearchPort catalogSearch,
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IApiHandler<SearchCatalogCommand, SearchCatalogResponse>
{
    public async Task<SearchCatalogResponse> Handle(
        SearchCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var local = await catalogSearch.SearchAsync(command, cancellationToken);
        var discovery = local.Discovery;

        if (!local.IsComplete && discovery is null)
        {
            var requested = command.ToCatalogSearchAttempt();
            var loaded = await DiscoveryHistory.LoadAsync(
                discoveryRepository,
                requested.SearchCriteria,
                cancellationToken);
            if (loaded.Aggregate.SearchRequested(requested))
            {
                await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
            }

            discovery = new EnrichmentDecision(
                WillBeLookedUp: true,
                Reason: "Local results incomplete",
                RetryAfterSeconds: null);
        }

        return new SearchCatalogResponse(
            command.Query,
            local.Results,
            discovery ?? new EnrichmentDecision(false, null, null));
    }
}
