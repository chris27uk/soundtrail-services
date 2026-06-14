using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Tracks;

public sealed class GetTrackHandler(ICatalogReadPort catalogReadPort) : IHandler<GetTrackCommand, TrackDetailsResponse?>
{
    public Task<TrackDetailsResponse?> Handle(GetTrackCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.GetTrackAsync(request.ArtistId, request.AlbumId, request.TrackId, cancellationToken);
}
