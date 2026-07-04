using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.GetTrack;

public sealed class GetTrackHandler(
    ICatalogReadPort catalogReadPort,
    ICommandBus commandBus) : IApiHandler<GetTrackCommand, TrackDetailsResponse?>
{
    public async Task<TrackDetailsResponse?> Handle(GetTrackCommand request, CancellationToken cancellationToken = default)
    {
        var response = await catalogReadPort.GetTrackAsync(request.ArtistId, request.AlbumId, request.TrackId, cancellationToken);
        await commandBus.SendAsync(
            new KnownTrackRequested(
                request.TrackId,
                request.Playback,
                DateTimeOffset.UtcNow,
                CorrelationId.New()),
            cancellationToken);
        return response;
    }
}
