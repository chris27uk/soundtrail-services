using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;

public sealed class GetTracksForPlaylistHandler(
    IGetTracksForPlaylistPort getTracksForPlaylistPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
{
    public async Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataMessage(
                new CatalogItemOperation.ChildTracksForPlaylist(request.PlaylistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await getTracksForPlaylistPort.GetTracksForPlaylistAsync(request.PlaylistId, cancellationToken);
    }
}
