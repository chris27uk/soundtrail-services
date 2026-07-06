using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetAlbum;

internal sealed class GetAlbumPortFake(GetAlbumResponse? response = null) : IGetAlbumPort
{
    public Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken) => Task.FromResult(response);
}
