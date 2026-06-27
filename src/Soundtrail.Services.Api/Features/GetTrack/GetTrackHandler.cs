using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.GetTrack;

public sealed class GetTrackHandler(ICatalogReadPort catalogReadPort) : IApiHandler<GetTrackCommand, TrackDetailsResponse?>
{
    public Task<TrackDetailsResponse?> Handle(GetTrackCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.GetTrackAsync(request.ArtistId, request.AlbumId, request.TrackId, cancellationToken);
}
