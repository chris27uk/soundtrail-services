using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.ListTracksByArtist;

public sealed class ListTracksByArtistHandler(
    ICatalogReadPort catalogReadPort,
    ICommandBus commandBus) : IApiHandler<ListTracksByArtistCommand, ArtistTracksResponse?>
{
    public async Task<ArtistTracksResponse?> Handle(ListTracksByArtistCommand request, CancellationToken cancellationToken = default)
    {
        var artist = await catalogReadPort.GetArtistAsync(request.ArtistId, cancellationToken);
        await commandBus.SendAsync(
            new KnownArtistRequested(
                request.ArtistId,
                DateTimeOffset.UtcNow,
                CorrelationId.New()),
            cancellationToken);
        if (artist is null)
        {
            return null;
        }

        var tracks = await catalogReadPort.ListTracksByArtistAsync(request.ArtistId, cancellationToken);
        return new ArtistTracksResponse(artist.ArtistId, artist.Name, tracks);
    }
}
