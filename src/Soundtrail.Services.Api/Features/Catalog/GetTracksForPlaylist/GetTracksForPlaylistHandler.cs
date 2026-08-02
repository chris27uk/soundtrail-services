using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Discovery;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;

public sealed class GetTracksForPlaylistHandler(
    IGetTracksForPlaylistPort getTracksForPlaylistPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
{
    public async Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        var dataRequest = NewDataRequestForPlaylist(request, requestedAt);
        await commandBus.SendAsync(dataRequest, cancellationToken);

        var response = await getTracksForPlaylistPort.GetTracksForPlaylistAsync(request.PlaylistId, cancellationToken);
        if (response is not null)
        {
            return response with
            {
                Discovery = PlaylistTracksDiscoveryFeedback
                    .FromProjection(response)
                    .EstimateAt(requestedAt)
            };
        }

        return GetTracksForPlaylistResponse.CatchingUp(request.PlaylistId, requestedAt);
    }

    private static RequestKnownMusicDataMessage NewDataRequestForPlaylist(GetTracksForPlaylistRequest request, DateTimeOffset requestedAt)
    {
        return new RequestKnownMusicDataMessage(
            new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId),
            LookupPriorityBand.High,
            100,
            0,
            requestedAt);
    }
}
