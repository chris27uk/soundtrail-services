using Microsoft.AspNetCore.TestHost;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForArtist;

internal sealed class GetTracksForArtistRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTracksForArtistRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetTracksForArtistRouteTestEnvironment ForExistingArtistTracks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>>(new GetTracksForArtistHandlerFake());
        var app = builder.Build();
        app.MapGetTracksForArtistEndpoints(AppTypeRegistry.ServiceLocation);
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTracksForArtistRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetTracksForArtistHandlerFake : IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>
    {
        public Task<GetTracksForArtistResponse?> Handle(GetTracksForArtistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTracksForArtistResponse?>(
                new GetTracksForArtistResponse(
                    ArtistId.From("artist-2501"),
                    ArtistName.From("The Artist"),
                    [
                        new GetTracksForArtistTrackResponse(
                            global::Soundtrail.Services.Tests.TestTrackIds.Create("track-2601"),
                            "The Track",
                            "The Artist",
                            "The Album",
                            201000,
                            "GBAYE2402601",
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/tracks/track-2601.jpg",
                            false,
                            [])
                    ]));
    }
}
