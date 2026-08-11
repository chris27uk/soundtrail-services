using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForAlbum;

internal sealed class GetTracksForAlbumRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTracksForAlbumRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => this.app.GetTestClient();

    public static GetTracksForAlbumRouteTestEnvironment ForExistingAlbumTracks()
    {
        var builder = WebApplication.CreateBuilder().Quiet();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>>(new GetTracksForAlbumHandlerFake());
        var app = builder.Build();
        app.MapGetTracksForAlbumEndpoints(AppTypeRegistry.ServiceLocation);
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTracksForAlbumRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        this.app.StopAsync().GetAwaiter().GetResult();
        this.app.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
