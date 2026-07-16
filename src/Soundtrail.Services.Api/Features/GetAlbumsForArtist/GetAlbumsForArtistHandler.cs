using Soundtrail.Adapters.Timing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Api.Features.GetAlbumsForArtist;

public sealed class GetAlbumsForArtistHandler(
    IGetAlbumsForArtistPort getAlbumsForArtistPort,
    ICommandBus commandBus,
    IClockPort clock) : IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>
{
    public async Task<GetAlbumsForArtistResponse?> Handle(GetAlbumsForArtistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new SearchForCatalogItemsCommand(
                new EnrichmentTarget.Existing(new CatalogItemId.Artist(request.ArtistId)),
                RequiredCatalogType.Albums,
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
