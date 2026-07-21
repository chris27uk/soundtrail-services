using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

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
        app.MapGetTracksForArtistEndpoints(new TypeRegistryFake());
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
                            new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-2601")),
                            "The Track",
                            "The Artist",
                            "The Album",
                            201000,
                            "GBAYE2402601",
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/tracks/track-2601.jpg")
                    ]));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetTracksForArtistResponse)domainObject;
            return new GetTracksForArtistResponseDto(
                response.ArtistId.Value,
                response.ArtistName.Value,
                response.Tracks.Select(
                        track => new GetTracksForArtistTrackResponseDto(
                            track.TrackId.Value,
                            track.MusicCatalogId.NormalisedIdentifier,
                            track.Title,
                            track.ArtistName,
                            track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl))
                    .ToArray());
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
