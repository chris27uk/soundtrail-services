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
    IClockPort clock) : IProjectionHandler<GetTracksForPlaylistRequest>
{
    public async Task HandleAsync(GetTracksForPlaylistRequest message, CancellationToken cancellationToken)
    {
        var requestedAt = clock.UtcNow;
        var dataRequest = NewDataRequestForPlaylist(message, requestedAt);
        await commandBus.SendAsync(dataRequest, cancellationToken);

        var response = await getTracksForPlaylistPort.GetTracksForPlaylistAsync(message.PlaylistId, cancellationToken);
        if (response is not null)
        {
            // Handle the response appropriately
        }
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
