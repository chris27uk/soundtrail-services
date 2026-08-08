using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

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
        app.MapGetAlbumEndpoints(AppTypeRegistry.ServiceLocation);
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
}
