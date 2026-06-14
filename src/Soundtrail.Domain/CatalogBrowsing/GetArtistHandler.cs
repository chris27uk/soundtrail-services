namespace Soundtrail.Domain.CatalogBrowsing;

public sealed class GetArtistHandler(ICatalogReadPort catalogReadPort) : IHandler<GetArtistCommand, ArtistDetailsResponse?>
{
    public Task<ArtistDetailsResponse?> Handle(GetArtistCommand request, CancellationToken cancellationToken = default) =>
        catalogReadPort.GetArtistAsync(request.ArtistId, cancellationToken);
}
