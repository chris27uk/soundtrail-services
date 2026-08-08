using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist;

public sealed class GetAlbumsForArtistHandler(
    IGetAlbumsForArtistPort getAlbumsForArtistPort,
    ICommandBus commandBus,
    IDiscoveryFeedbackPort discoveryFeedbackPort,
    IClockPort clock) : IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>
{
    public async Task<GetAlbumsForArtistResponse?> Handle(GetAlbumsForArtistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataMessage(
                new CatalogItemOperation.ChildAlbumsForArtist(request.ArtistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            { },
            cancellationToken);

        var response = await getAlbumsForArtistPort.GetAlbumsForArtistAsync(request.ArtistId, cancellationToken);
        var discovery = await discoveryFeedbackPort.GetAsync(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildAlbumsForArtist(request.ArtistId)),
            cancellationToken);

        if (response is not null)
        {
            return response with { Discovery = discovery };
        }

        return discovery is null
            ? null
            : new GetAlbumsForArtistResponse(
                request.ArtistId,
                ArtistName.Empty,
                [],
                discovery);
    }
}
