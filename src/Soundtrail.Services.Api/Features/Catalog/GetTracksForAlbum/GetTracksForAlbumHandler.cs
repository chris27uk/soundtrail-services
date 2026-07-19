using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum;

public sealed class GetTracksForAlbumHandler(
    IGetTracksForAlbumPort getTracksForAlbumPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>
{
    public async Task<GetTracksForAlbumResponse?> Handle(GetTracksForAlbumRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataCommand(
                new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await getTracksForAlbumPort.GetTracksForAlbumAsync(request.AlbumId, cancellationToken);
    }
}
