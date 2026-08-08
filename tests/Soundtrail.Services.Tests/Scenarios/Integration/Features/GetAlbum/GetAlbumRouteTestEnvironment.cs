using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Features.GetAlbum;

internal sealed class GetAlbumRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetAlbumRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => this.app.GetTestClient();

    public static GetAlbumRouteTestEnvironment ForExistingAlbum(string artistId, string albumId)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetAlbumRequest, GetAlbumResponse?>>(
            new GetAlbumHandlerFake(artistId, albumId));
        var app = builder.Build();
        app.MapGetAlbumEndpoints(AppTypeRegistry.ServiceLocation);
        app.StartAsync().GetAwaiter().GetResult();
        return new GetAlbumRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        this.app.StopAsync().GetAwaiter().GetResult();
        this.app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetAlbumHandlerFake(string artistId, string albumId)
        : IApiHandler<GetAlbumRequest, GetAlbumResponse?>
    {
        public Task<GetAlbumResponse?> Handle(GetAlbumRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetAlbumResponse?>(
                new GetAlbumResponse(
                    ArtistId.From(artistId),
                    ArtistName.From("The Artist"),
                    AlbumId.From(artistId, albumId),
                    "The Album",
                    new DateOnly(2024, 6, 7)));
    }
}
