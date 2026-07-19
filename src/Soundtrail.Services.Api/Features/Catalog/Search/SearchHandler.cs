using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search;

public sealed class SearchHandler(
    ISearchPort searchPort,
    ICommandBus commandBus,
    IDiscoveryFeedbackPort discoveryFeedbackPort,
    IClockPort clock) : IApiHandler<SearchRequest, SearchResponse?>
{
    public async Task<SearchResponse?> Handle(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var searchCriteria = new SearchCriteria(request.QueryText, request.Filter);
        var requestedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new RequestUnknownMusicDataCommand(
                searchCriteria,
                LookupPriorityBand.High,
                100,
                0,
                requestedAt),
            cancellationToken);

        var response = await searchPort.SearchAsync(searchCriteria, cancellationToken);
        if (response is null)
        {
            return null;
        }

        var discovery = await discoveryFeedbackPort.GetAsync(
            new EnrichmentTarget.SearchForUnknownCatalogItem(searchCriteria),
            cancellationToken);

        return response with { Discovery = discovery };
    }
}
