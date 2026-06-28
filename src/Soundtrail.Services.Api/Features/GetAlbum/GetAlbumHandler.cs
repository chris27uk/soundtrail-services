using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.GetAlbum;

public sealed class GetAlbumHandler(
    ICatalogReadPort catalogReadPort,
    ICommandBus commandBus) : IApiHandler<GetAlbumCommand, AlbumDetailsResponse?>
{
    public async Task<AlbumDetailsResponse?> Handle(GetAlbumCommand request, CancellationToken cancellationToken = default)
    {
        var response = await catalogReadPort.GetAlbumAsync(request.ArtistId, request.AlbumId, cancellationToken);
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
