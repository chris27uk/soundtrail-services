using Soundtrail.Domain;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.SearchCatalog.Support;

namespace Soundtrail.Services.Api.Features.SearchCatalog;

public sealed class SearchCatalogHandler(
    ICatalogSearchPort catalogSearch,
    CatalogSearchAttemptRecorder catalogSearchAttemptRecorder) : IApiHandler<SearchCatalogCommand, SearchCatalogResponse>
{
    public async Task<SearchCatalogResponse> Handle(
        SearchCatalogCommand command,
        CancellationToken cancellationToken = default)
    {
        var local = await catalogSearch.SearchAsync(command, cancellationToken);
        var discovery = local.Discovery;

        if (!local.IsComplete && discovery is null)
        {
            var criteria = command.ToCatalogSearchCriteria();
            await catalogSearchAttemptRecorder.TryRequestAsync(
                new RecordCatalogSearchAttemptCommand(
                    criteria,
                    command.Query.ToNewCatalogSearchAttempt(criteria)),
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
