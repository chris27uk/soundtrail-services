using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum.Support;

internal sealed class GetTracksForAlbumPortFake : IGetTracksForAlbumPort
{
    private readonly Func<AlbumId, CancellationToken, Task<GetTracksForAlbumResponse?>>? resolver;
    private GetTracksForAlbumResponse? response;

    public GetTracksForAlbumPortFake(GetTracksForAlbumResponse? response = null) => this.response = response;

    private GetTracksForAlbumPortFake(
        Func<AlbumId, CancellationToken, Task<GetTracksForAlbumResponse?>> resolver) =>
        this.resolver = resolver;

    public List<AlbumId> RequestedAlbumIds { get; } = [];

    public static GetTracksForAlbumPortFake Create(
        Func<AlbumId, CancellationToken, Task<GetTracksForAlbumResponse?>> resolver) =>
        new(resolver);

    public void Seed(GetTracksForAlbumResponse? tracks) => this.response = tracks;

    public Task<GetTracksForAlbumResponse?> GetTracksForAlbumAsync(AlbumId albumId, CancellationToken cancellationToken)
    {
        RequestedAlbumIds.Add(albumId);
        return this.resolver is null
            ? Task.FromResult(this.response)
            : this.resolver(albumId, cancellationToken);
    }
}
