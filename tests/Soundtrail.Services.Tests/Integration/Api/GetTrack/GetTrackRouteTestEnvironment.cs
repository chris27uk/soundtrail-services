using Soundtrail.Domain.Catalog;
using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Contract;
using Soundtrail.Services.Api.Features.GetTrack.Registrations;

namespace Soundtrail.Services.Tests.Integration.Api.GetTrack;

internal sealed class GetTrackRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTrackRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetTrackRouteTestEnvironment ForExistingTrack()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetTrackRequest, GetTrackResponse?>>(new GetTrackHandlerFake());
        var app = builder.Build();
        app.MapGetTrackEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTrackRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetTrackHandlerFake : IApiHandler<GetTrackRequest, GetTrackResponse?>
    {
        public Task<GetTrackResponse?> Handle(GetTrackRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTrackResponse?>(
                new GetTrackResponse(
                    global::Soundtrail.Services.Tests.TestTrackIds.Create("track-501"),
                    new CatalogItemId.Track(global::Soundtrail.Services.Tests.TestTrackIds.Create("track-501")),
                    "The Track",
                    "The Artist",
                    "The Album",
                    201000,
                    "GBAYE2400301",
                    new DateOnly(2024, 6, 7),
                    "https://cdn.soundtrail.test/tracks/mc_track_501.jpg"));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetTrackResponse)domainObject;
            return new GetTrackResponseDto(
                response.TrackId.Value,
                response.MusicCatalogId.NormalisedIdentifier,
                response.Title,
                response.ArtistName,
                response.AlbumTitle,
                response.DurationMs,
                response.Isrc,
                response.ReleaseDate,
                response.ArtworkUrl);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
