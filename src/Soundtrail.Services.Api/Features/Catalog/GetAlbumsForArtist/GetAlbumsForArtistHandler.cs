using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist;

public sealed class GetAlbumsForArtistHandler(
    IGetAlbumsForArtistPort getAlbumsForArtistPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>
{
    public async Task<GetAlbumsForArtistResponse?> Handle(GetAlbumsForArtistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataCommand(
                new CatalogItemOperation.ChildAlbumsForArtist(request.ArtistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        return await getAlbumsForArtistPort.GetAlbumsForArtistAsync(request.ArtistId, cancellationToken);
    }
}
