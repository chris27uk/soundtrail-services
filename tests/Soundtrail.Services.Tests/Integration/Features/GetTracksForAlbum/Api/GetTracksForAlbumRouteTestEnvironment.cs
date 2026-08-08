using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetTracksForAlbum.Api;

internal sealed class GetTracksForAlbumRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTracksForAlbumRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetTracksForAlbumRouteTestEnvironment ForExistingAlbumTracks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>>(new GetTracksForAlbumHandlerFake());
        var app = builder.Build();
        app.MapGetTracksForAlbumEndpoints(AppTypeRegistry.ServiceLocation);
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTracksForAlbumRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetTracksForAlbumHandlerFake : IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>
    {
        public Task<GetTracksForAlbumResponse?> Handle(GetTracksForAlbumRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTracksForAlbumResponse?>(
                new GetTracksForAlbumResponse(
                    ArtistId.From("artist-801"),
                    AlbumId.From("artist-801", "album-901"),
                    "The Album",
                    [
                        new GetTracksForAlbumTrackResponse(
                            global::Soundtrail.Services.Tests.TestTrackIds.Create("track-1001"),
                            "The Track",
                            "The Artist",
                            201000,
                            "GBAYE2401001",
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/tracks/track-1001.jpg",
                            false,
                            [])
                    ]));
    }
}
