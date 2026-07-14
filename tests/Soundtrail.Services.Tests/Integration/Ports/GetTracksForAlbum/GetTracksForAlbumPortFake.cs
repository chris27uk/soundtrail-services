using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForAlbum;

internal sealed class GetTracksForAlbumPortFake(GetTracksForAlbumResponse? response = null) : IGetTracksForAlbumPort
{
    public Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken) => Task.FromResult(response);
}
