using Soundtrail.Adapters.Timing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
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
        var searchCriteria = new SearchCriteria(request.QueryText, request.Filter);
        var requestedAt = clock.UtcNow;

        await commandBus.SendAsync(
            new RequestUnknownMusicDataCommand(
                searchCriteria,
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await searchPort.SearchAsync(searchCriteria, cancellationToken);
    }
}
