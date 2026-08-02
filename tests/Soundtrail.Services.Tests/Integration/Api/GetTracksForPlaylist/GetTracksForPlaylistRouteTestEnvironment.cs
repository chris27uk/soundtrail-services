using System.Text.Json.Serialization;
using Microsoft.AspNetCore.TestHost;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistRouteTestEnvironment : IDisposable
{
    private readonly WebApplication app;

    private GetTracksForPlaylistRouteTestEnvironment(WebApplication app)
    {
        this.app = app;
    }

    public HttpClient Client => app.GetTestClient();

    public static GetTracksForPlaylistRouteTestEnvironment ForExistingPlaylistTracks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddSingleton<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>>(new GetTracksForPlaylistHandlerFake());
        var app = builder.Build();
        app.MapGetTracksForPlaylistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTracksForPlaylistRouteTestEnvironment(app);
    }

    public static GetTracksForPlaylistRouteTestEnvironment ForMissingPlaylistTracks()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        builder.Services.AddSingleton<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>>(new MissingGetTracksForPlaylistHandlerFake());
        var app = builder.Build();
        app.MapGetTracksForPlaylistEndpoints(new TypeRegistryFake());
        app.StartAsync().GetAwaiter().GetResult();
        return new GetTracksForPlaylistRouteTestEnvironment(app);
    }

    public void Dispose()
    {
        app.StopAsync().GetAwaiter().GetResult();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class GetTracksForPlaylistHandlerFake : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
    {
        public Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTracksForPlaylistResponse?>(
                new GetTracksForPlaylistResponse(
                    PlaylistId.FromPlaylistName("WorldwideSongChart"),
                    [
                        new GetTracksForPlaylistTrackResponse(
                            global::Soundtrail.Services.Tests.TestTrackIds.Create("track-3401"),
                            "The Track",
                            "The Artist",
                            "The Album",
                            201000,
                            null,
                            new DateOnly(2024, 6, 7),
                            "https://cdn.soundtrail.test/tracks/track-3401.jpg",
                            false,
                            [])
                    ],
                    new DiscoveryFeedbackResponse(
                        "scheduled",
                        LookupPriorityBand.High,
                        DateTimeOffset.UtcNow.AddSeconds(60),
                        DateTimeOffset.UtcNow.AddSeconds(120),
                        "Playlist lookup queued.",
                        DateTimeOffset.UtcNow)));
    }

    private sealed class MissingGetTracksForPlaylistHandlerFake : IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>
    {
        public Task<GetTracksForPlaylistResponse?> Handle(GetTracksForPlaylistRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetTracksForPlaylistResponse?>(null);
    }

    private sealed class TypeRegistryFake : ITypeRegistry
    {
        public TDto ToDto<TDto>(object domainObject) where TDto : class => (ToDto(domainObject) as TDto)!;

        public object ToDto(object domainObject)
        {
            var response = (GetTracksForPlaylistResponse)domainObject;
            return new GetTracksForPlaylistResponseDto(
                response.PlaylistId.Value,
                    response.Tracks.Select(
                        track => new GetTracksForPlaylistTrackResponseDto(
                            track.TrackId.Value,
                            track.Title,
                            track.ArtistName,
                            track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl,
                            track.Playable,
                            []))
                    .ToArray(),
                response.Discovery is null
                    ? null
                    : new DiscoveryFeedbackResponseDto(
                        response.Discovery.Status,
                        response.Discovery.Priority.ToString(),
                        response.Discovery.NextEligibleAt,
                        response.Discovery.EarliestExpectedCompletionAt,
                        response.Discovery.Reason,
                        response.Discovery.UpdatedAtUtc));
        }

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => throw new NotSupportedException();

        public object ToDomainObject(object? dto) => throw new NotSupportedException();

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
