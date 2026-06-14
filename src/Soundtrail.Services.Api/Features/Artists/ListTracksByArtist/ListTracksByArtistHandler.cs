using Soundtrail.Domain;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.Artists;

public sealed class ListTracksByArtistHandler(ICatalogReadPort catalogReadPort) : IHandler<ListTracksByArtistCommand, ArtistTracksResponse?>
{
    public async Task<ArtistTracksResponse?> Handle(ListTracksByArtistCommand request, CancellationToken cancellationToken = default)
    {
        var artist = await catalogReadPort.GetArtistAsync(request.ArtistId, cancellationToken);
        if (artist is null)
        {
            return null;
        }

        var tracks = await catalogReadPort.ListTracksByArtistAsync(request.ArtistId, cancellationToken);
        return new ArtistTracksResponse(artist.ArtistId, artist.Name, tracks);
    }
}
