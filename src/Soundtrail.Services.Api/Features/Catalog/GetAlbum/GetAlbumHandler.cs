using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.GetAlbum;

public sealed class GetAlbumHandler(IGetAlbumPort getAlbumPort) : IApiHandler<GetAlbumRequest, GetAlbumResponse?>
{
    public async Task<GetAlbumResponse?> Handle(GetAlbumRequest request, CancellationToken cancellationToken = default) => await getAlbumPort.GetAlbumAsync(request.AlbumId, cancellationToken);
}
