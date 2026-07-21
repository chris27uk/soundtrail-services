using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Api.GetAlbum;

internal sealed class GetAlbumRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetAlbumRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetAlbumRouteTestEnvironment ForExistingAlbum()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetAlbumRequest, GetAlbumResponse?>>(new GetAlbumHandlerFake());
        var app = builder.Build();
        app.MapGetAlbumEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetAlbumRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetAlbumHandlerFake : IApiHandler<GetAlbumRequest, GetAlbumResponse?>
    {
        public Task<GetAlbumResponse?> Handle(GetAlbumRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetAlbumResponse?>(
                new GetAlbumResponse(
                    ArtistId.From("artist-301"),
                    ArtistName.From("The Artist"),
                    AlbumId.From("artist-301", "album-501"),
                    "The Album",
                    new DateOnly(2024, 6, 7)));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetAlbumResponse)domainObject;
            return new GetAlbumResponseDto(
                response.ArtistId.Value,
                response.ArtistName.Value,
                response.AlbumId.ArtistAlbumId,
                response.ReleaseDate);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
