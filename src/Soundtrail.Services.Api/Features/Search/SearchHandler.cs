using Soundtrail.Adapters.Timing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Api.Features.Search;

public sealed class SearchHandler(
    ISearchPort searchPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<SearchRequest, SearchResponse?>
{
    public async Task<SearchResponse?> Handle(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var searchCriteria = new SearchCriteria(request.QueryText, MapSearchTypes(request.Filter));
        var requestedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new SearchForCatalogItemsCommand(
                new EnrichmentFilter.SearchCriteria(searchCriteria),
                RequiredCatalogType.None,
                LookupPriorityBandDto.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await searchPort.SearchAsync(searchCriteria, cancellationToken);
    }

    private static SearchTypes MapSearchTypes(SearchFilter filter) =>
        filter switch
        {
            SearchFilter.Artist => SearchTypes.Artist,
            SearchFilter.Album => SearchTypes.Album,
            SearchFilter.Track => SearchTypes.Track,
            _ => throw new InvalidOperationException($"Unsupported search filter '{filter}'.")
        };
}
