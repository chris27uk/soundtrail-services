using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForAlbum;

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
        app.MapGetTracksForAlbumEndpoints(new TypeRegistryFake());
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
                            new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-1001")),
                            "The Track",
                            "The Artist",
                            201000,
                            "GBAYE2401001",
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/tracks/track-1001.jpg")
                    ]));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetTracksForAlbumResponse)domainObject;
            return new GetTracksForAlbumResponseDto(
                response.ArtistId.Value,
                response.AlbumId.ArtistAlbumId,
                response.AlbumTitle,
                response.Tracks.Select(
                        track => new GetTracksForAlbumTrackResponseDto(
                            track.TrackId.Value,
                            track.MusicCatalogId.NormalisedIdentifier,
                            track.Title,
                            track.ArtistName,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl))
                    .ToArray(),
                null);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
