using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

namespace Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist;

public sealed class GetTracksForArtistHandler(
    IGetTracksForArtistPort getTracksForArtistPort,
    ICommandBus commandBus,
    IDiscoveryFeedbackPort discoveryFeedbackPort,
    IClockPort clock) : IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>
{
    public async Task<GetTracksForArtistResponse?> Handle(GetTracksForArtistRequest request, CancellationToken cancellationToken = default)
    {
        var requestedAt = clock.UtcNow;
        await commandBus.SendAsync(
            new RequestKnownMusicDataMessage(
                new CatalogItemOperation.ChildTracksForArtist(request.ArtistId),
                LookupPriorityBand.High,
                100,
                0,
                requestedAt)
            { },
            cancellationToken);

        var response = await getTracksForArtistPort.GetTracksForArtistAsync(request.ArtistId, cancellationToken);
        var discovery = await discoveryFeedbackPort.GetAsync(
            new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(request.ArtistId)),
            cancellationToken);

        if (response is not null)
        {
            return response with { Discovery = discovery };
        }

        return discovery is null
            ? null
            : new GetTracksForArtistResponse(
                request.ArtistId,
                ArtistName.Empty,
                [],
                discovery);
    }
}
