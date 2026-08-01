using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum;

public sealed class GetTracksForAlbumHandler(
    IGetTracksForAlbumPort getTracksForAlbumPort,
    ICommandBus commandBus,
    IDiscoveryFeedbackPort discoveryFeedbackPort,
    IClockPort clock) : IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>
{
    public async Task<GetTracksForAlbumResponse?> Handle(GetTracksForAlbumRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataMessage(
                new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            {
                CreatedAt = requestedAt
            },
            cancellationToken);

        var response = await getTracksForAlbumPort.GetTracksForAlbumAsync(request.AlbumId, cancellationToken);
        var discovery = await discoveryFeedbackPort.GetAsync(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForAlbum(request.AlbumId)),
            cancellationToken);

        if (response is not null)
        {
            return response with { Discovery = discovery };
        }

        return discovery is null
            ? null
            : new GetTracksForAlbumResponse(
                ArtistId.From(request.AlbumId.ArtistId),
                request.AlbumId,
                string.Empty,
                [],
                discovery);
    }
}
