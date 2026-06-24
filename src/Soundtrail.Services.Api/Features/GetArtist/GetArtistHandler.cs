using Soundtrail.Domain;
using Soundtrail.Services.Api.Infrastructure.Ports;
using Soundtrail.Domain.CatalogBrowsing;

namespace Soundtrail.Services.Api.Features.GetArtist;

public sealed class GetArtistHandler(ICatalogReadPort catalogReadPort) : IApiHandler<GetArtistCommand, ArtistDetailsResponse?>
{
    public Task<ArtistDetailsResponse?> Handle(GetArtistCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.GetArtistAsync(request.ArtistId, cancellationToken);
}
