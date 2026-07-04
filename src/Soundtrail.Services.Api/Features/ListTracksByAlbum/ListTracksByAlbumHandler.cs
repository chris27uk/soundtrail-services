using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.ListTracksByAlbum;

public sealed class ListTracksByAlbumHandler(
    ICatalogReadPort catalogReadPort,
    ICommandBus commandBus) : IApiHandler<ListTracksByAlbumCommand, AlbumTracksResponse?>
{
    public async Task<AlbumTracksResponse?> Handle(ListTracksByAlbumCommand request, CancellationToken cancellationToken = default)
    {
        var response = await catalogReadPort.ListTracksByAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
        await commandBus.SendAsync(
            new KnownAlbumRequested(
                request.ArtistId,
                request.AlbumId,
                DateTimeOffset.UtcNow,
                CorrelationId.New()),
            cancellationToken);
        return response;
    }
}
