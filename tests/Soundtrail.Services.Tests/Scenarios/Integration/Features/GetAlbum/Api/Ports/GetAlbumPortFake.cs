using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.GetAlbum.Api.Ports;

internal sealed class GetAlbumPortFake : IGetAlbumPort
{
    private GetAlbumResponse? response;

    public GetAlbumPortFake(GetAlbumResponse? response = null) => this.response = response;

    public List<AlbumId> RequestedAlbumIds { get; } = [];

    public void Seed(GetAlbumResponse? album) => response = album;

    public Task<GetAlbumResponse?> GetAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
    {
        RequestedAlbumIds.Add(albumId);
        return Task.FromResult(response);
    }
}
