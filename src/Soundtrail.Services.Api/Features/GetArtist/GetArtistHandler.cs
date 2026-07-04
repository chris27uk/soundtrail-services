using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Services.Api.Infrastructure.Ports;

namespace Soundtrail.Services.Api.Features.GetArtist;

public sealed class GetArtistHandler(
    ICatalogReadPort catalogReadPort,
    ICommandBus commandBus) : IApiHandler<GetArtistCommand, ArtistDetailsResponse?>
{
    public async Task<ArtistDetailsResponse?> Handle(GetArtistCommand request, CancellationToken cancellationToken = default)
    {
        var response = await catalogReadPort.GetArtistAsync(request.ArtistId, cancellationToken);
        await commandBus.SendAsync(
            new KnownArtistRequested(
                request.ArtistId,
                DateTimeOffset.UtcNow,
                CorrelationId.New()),
            cancellationToken);
        return response;
    }
}
