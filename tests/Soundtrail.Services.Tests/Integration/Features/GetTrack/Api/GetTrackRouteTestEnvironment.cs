using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.GetTrack.Api;

internal sealed class GetTrackRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTrackRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetTrackRouteTestEnvironment ForExistingTrack(string trackId)
    {
        var builder = WebApplication.CreateBuilder().Quiet();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IApiHandler<GetTrackRequest, GetTrackResponse?>>(
            new GetTrackHandlerFake(trackId));
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

    private sealed class GetTrackHandlerFake(string trackId) : IApiHandler<GetTrackRequest, GetTrackResponse?>
    {
        public Task<GetTrackResponse?> Handle(GetTrackRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTrackResponse?>(
                new GetTrackResponse(
                    TrackId.From(trackId),
                    "The Track",
                    "The Artist",
                    "The Album",
                    201000,
                    "GBAYE2400301",
                    new DateOnly(2024, 6, 7),
                    $"https://cdn.soundtrail.test/tracks/{trackId}.jpg",
                    false,
                    []));
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetTrackResponse)domainObject;
            return new GetTrackResponseDto(
                response.TrackId.Value,
                response.Title,
                response.ArtistName,
                response.AlbumTitle,
                response.DurationMs,
                response.Isrc,
                response.ReleaseDate,
                response.ArtworkUrl,
                response.Playable,
                [],
                null);
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
