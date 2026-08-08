using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;

namespace Soundtrail.Services.Tests.Integration.Features.GetAlbumsForArtist.Support;

internal sealed class GetAlbumsForArtistPortFake : IGetAlbumsForArtistPort
{
    private readonly Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>>? resolver;
    private GetAlbumsForArtistResponse? response;

    public GetAlbumsForArtistPortFake(GetAlbumsForArtistResponse? response = null) => this.response = response;

    private GetAlbumsForArtistPortFake(
        Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>> resolver) =>
        this.resolver = resolver;

    public List<ArtistId> RequestedArtistIds { get; } = [];

    public static GetAlbumsForArtistPortFake Create(
        Func<ArtistId, CancellationToken, Task<GetAlbumsForArtistResponse?>> resolver) =>
        new(resolver);

    public void Seed(GetAlbumsForArtistResponse? albums) => this.response = albums;

    public Task<GetAlbumsForArtistResponse?> GetAlbumsForArtistAsync(ArtistId artistId, CancellationToken cancellationToken)
    {
        RequestedArtistIds.Add(artistId);
        return this.resolver is null
            ? Task.FromResult(this.response)
            : this.resolver(artistId, cancellationToken);
    }
}
