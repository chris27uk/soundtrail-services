using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Adapters;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Api.Features.GetAlbumsForArtist.Registrations;

namespace Soundtrail.Services.Tests.Integration.Api.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetAlbumsForArtistRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetAlbumsForArtistRouteTestEnvironment ForExistingArtistAlbums()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>>(new GetAlbumsForArtistHandlerFake());
        var app = builder.Build();
        app.MapGetAlbumsForArtistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetAlbumsForArtistRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetAlbumsForArtistHandlerFake : IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>
    {
        public Task<GetAlbumsForArtistResponse?> Handle(GetAlbumsForArtistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetAlbumsForArtistResponse?>(
                new GetAlbumsForArtistResponse(
                    ArtistId.From("artist-1901"),
                    ArtistName.From("The Artist"),
                    [
                        new GetAlbumsForArtistAlbumResponse(
                            AlbumId.From("artist-1901", "album-2001"),
                            new MusicCatalogId.Album(AlbumId.From("artist-1901", "album-2001")),
                            "The Album",
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/albums/album-2001.jpg")
                    ]));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetAlbumsForArtistResponse)domainObject;
            return new GetAlbumsForArtistResponseDto(
                response.ArtistId.Value,
                response.ArtistName.Value,
                response.Albums.Select(
                        album => new GetAlbumsForArtistAlbumResponseDto(
                            album.AlbumId.ArtistAlbumId,
                            album.MusicCatalogId.NormalisedIdentifier,
                            album.AlbumTitle,
                            album.ReleaseDate,
                            album.ArtworkUrl))
                    .ToArray());
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
