using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Soundtrail.Adapters.Timing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Integration.Ports;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Soundtrail.Services.Tests.Integration.Api.GetTracksForPlaylist;

internal sealed class WorldTop100PlaylistScenarioTestEnvironment : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly WireMockServer wireMockServer;
    private readonly IDocumentStore documentStore;
    private readonly HttpClient client;
    private readonly List<string> cleanupDocumentIds = [];

    private WorldTop100PlaylistScenarioTestEnvironment(
        WebApplication app,
        WireMockServer wireMockServer,
        IDocumentStore documentStore,
        HttpClient client,
        CommandBusFake commandBus,
        ClockFake clock)
    {
        this.app = app;
        this.wireMockServer = wireMockServer;
        this.documentStore = documentStore;
        this.client = client;
        CommandBus = commandBus;
        Clock = clock;
    }

    public HttpClient Client => client;

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public PlaylistId PlaylistId => PlaylistId.FromPlaylistName("world_top_100");

    public static async Task<WorldTop100PlaylistScenarioTestEnvironment> CreateAsync()
    {
        var documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
        var wireMockServer = WireMockServer.Start();

        ConfigureKworbStub(wireMockServer);
        ConfigureMusicbrainzStub(wireMockServer);
        ConfigureOdesliStub(wireMockServer);

        var commandBus = new CommandBusFake();
        var clock = new ClockFake();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(commandBus);
        builder.Services.AddSingleton<ICommandBus>(sp => sp.GetRequiredService<CommandBusFake>());
        builder.Services.AddSingleton(clock);
        builder.Services.AddSingleton<IClockPort>(sp => sp.GetRequiredService<ClockFake>());
        builder.Services.AddSingleton<ITypeRegistry, TypeRegistryFake>();
        builder.Services.AddSingleton(documentStore);
        builder.Services.AddSingleton<IGetTracksForPlaylistPort>(sp =>
            new RavenGetTracksForPlaylistPort(
                sp.GetRequiredService<IDocumentStore>(),
                sp.GetRequiredService<ITypeRegistry>()));
        builder.Services.AddSingleton<IDiscoveryFeedbackPort>(sp =>
            new RavenDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>()));
        builder.Services.AddSingleton<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>>(sp =>
            new GetTracksForPlaylistHandler(
                sp.GetRequiredService<IGetTracksForPlaylistPort>(),
                sp.GetRequiredService<ICommandBus>(),
                sp.GetRequiredService<IDiscoveryFeedbackPort>(),
                sp.GetRequiredService<IClockPort>()));

        var app = builder.Build();
        app.MapGetTracksForPlaylistEndpoints(app.Services.GetRequiredService<ITypeRegistry>());
        app.StartAsync().GetAwaiter().GetResult();

        var environment = new WorldTop100PlaylistScenarioTestEnvironment(
            app,
            wireMockServer,
            documentStore,
            app.GetTestClient(),
            commandBus,
            clock);
        await environment.ResetScenarioStateAsync();

        return environment;
    }

    public async Task SeedPendingDiscoveryAsync()
    {
        var targetId = new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId).StableIdentifier();
        var documentId = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId);
        TrackForCleanup(documentId);

        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogDiscoveryFeedbackRecordDto
            {
                Id = documentId,
                TargetId = targetId,
                Status = "scheduled",
                Priority = LookupPriorityBand.High.ToString(),
                NextEligibleAtUtc = Clock.UtcNow.AddSeconds(15),
                EarliestExpectedCompletionAtUtc = Clock.UtcNow.AddSeconds(75),
                Reason = "Playlist backfill and metadata lookups are still running.",
                UpdatedAtUtc = Clock.UtcNow
            });
        await session.SaveChangesAsync();
    }

    private async Task ResetScenarioStateAsync()
    {
        var discoveryDocumentId = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(
            new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId).StableIdentifier());
        var playlistDocumentId = CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value);

        await EmbeddedRavenTestServer.DisposeAsync(documentStore, discoveryDocumentId);
        await EmbeddedRavenTestServer.DisposeAsync(documentStore, playlistDocumentId);
    }

    public async Task<StreamingCoverageSummary> MaterializeResolvedScenarioAsync()
    {
        var kworbPort = new KworbPlaylistTracksPort(CreateExternalClient());
        var musicbrainzPort = new MusicbrainzCatalogSearchPort(
            CreateExternalClient(),
            Options.Create(new MusicBrainzOptions
            {
                BaseUrl = wireMockServer.Url!,
                UserAgent = "Soundtrail.Tests/1.0"
            }));
        var odesliPort = new OdesliStreamingLocationPort(
            CreateExternalClient(),
            Options.Create(new OdesliOptions
            {
                BaseUrl = wireMockServer.Url!,
                UserCountry = "US"
            }));

        var references = await kworbPort.ReadAsync(PlaylistId, ProviderName.Spotify, CancellationToken.None);
        var requestedTrackIds = references
            .Select(reference => TrackId.TryCreate(reference.ArtistName.Value, reference.TrackTitle))
            .OfType<TrackIdCreateResult.Success>()
            .Select(result => result.Value)
            .ToArray();

        foreach (var trackId in requestedTrackIds)
        {
            TrackForCleanup(CatalogTrackRecordDto.GetDocumentId(trackId.Value));
        }

        var streamingCoverage = new Dictionary<string, bool>(StringComparer.Ordinal);

        using (var session = documentStore.OpenAsyncSession())
        {
            foreach (var reference in references)
            {
                var entries = await musicbrainzPort.ReadAsync(
                    new SearchCriteria($"{reference.TrackTitle} {reference.ArtistName.Value}", SearchType.Track),
                    CancellationToken.None);

                foreach (var entry in entries)
                {
                    if (entry.Item is not CatalogItem.MusicTrack(var track))
                    {
                        continue;
                    }

                    var streamingLocations = await ReadStreamingLocationsAsync(odesliPort, track);
                    streamingCoverage[track.TrackId.Value] = streamingLocations.Length > 0;

                    await session.StoreAsync(
                        new CatalogTrackRecordDto
                        {
                            Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                            TrackId = track.TrackId.Value,
                            MusicCatalogId = track.TrackId.Value,
                            Title = track.Title,
                            ArtistName = track.ArtistName,
                            AlbumTitle = track.AlbumTitle,
                            DurationMs = track.DurationMs,
                            Isrc = track.Isrc,
                            ReleaseDate = track.ReleaseDate,
                            ReleaseType = track.ReleaseType,
                            ArtworkUrl = track.ArtworkUrl,
                            StreamingLocations = streamingLocations,
                            UpdatedAt = track.UpdatedAt
                        });
                }
            }

            await session.SaveChangesAsync();
        }

        var playlistDocumentId = CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value);
        TrackForCleanup(playlistDocumentId);

        var readModelPort = new RavenStorePlaylistTracksReadModelPort(documentStore);
        await readModelPort.StoreAsync(
            new PlaylistTracksDiscovered(PlaylistId, requestedTrackIds, Clock.UtcNow),
            CancellationToken.None);

        var targetId = new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId).StableIdentifier();
        var discoveryDocumentId = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(targetId);
        TrackForCleanup(discoveryDocumentId);

        using (var session = documentStore.OpenAsyncSession())
        {
            await session.StoreAsync(
                new CatalogDiscoveryFeedbackRecordDto
                {
                    Id = discoveryDocumentId,
                    TargetId = targetId,
                    Status = "completed",
                    Priority = LookupPriorityBand.High.ToString(),
                    NextEligibleAtUtc = null,
                    EarliestExpectedCompletionAtUtc = null,
                    Reason = "Playlist metadata has been materialized from local WireMock services.",
                    UpdatedAtUtc = Clock.UtcNow.AddMinutes(1)
                });

            foreach (var (trackIdValue, hasStreamingLocation) in streamingCoverage)
            {
                var streamingTargetId = new CatalogItemOperation.StreamingLocationForTrack(TrackId.From(trackIdValue))
                    .StableIdentifier();
                var streamingDiscoveryDocumentId = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(streamingTargetId);
                TrackForCleanup(streamingDiscoveryDocumentId);

                await session.StoreAsync(
                    new CatalogDiscoveryFeedbackRecordDto
                    {
                        Id = streamingDiscoveryDocumentId,
                        TargetId = streamingTargetId,
                        Status = hasStreamingLocation ? "completed" : "attempt-failed",
                        Priority = LookupPriorityBand.High.ToString(),
                        NextEligibleAtUtc = null,
                        EarliestExpectedCompletionAtUtc = null,
                        Reason = hasStreamingLocation
                            ? "Streaming locations have been materialized from local WireMock services."
                            : "Streaming locations were not found in local WireMock services.",
                        UpdatedAtUtc = Clock.UtcNow.AddMinutes(1)
                    });
            }

            await session.SaveChangesAsync();
        }

        return new StreamingCoverageSummary(streamingCoverage);
    }

    private static async Task<CatalogStreamingLocationRecordDto[]> ReadStreamingLocationsAsync(
        OdesliStreamingLocationPort odesliPort,
        Track track)
    {
        var locations = new List<CatalogStreamingLocationRecordDto>();

        foreach (var provider in ProviderName.All)
        {
            var link = await odesliPort.ReadByTrackMetadataAsync(
                track.ArtistName,
                track.Title,
                provider,
                CancellationToken.None);

            if (link is null)
            {
                continue;
            }

            locations.Add(new CatalogStreamingLocationRecordDto
            {
                Provider = provider.StableValue,
                ExternalId = null,
                Url = link.ToString()
            });
        }

        return locations.ToArray();
    }

    public async Task<GetTracksForPlaylistResponseDto?> GetPlaylistAsync()
    {
        var response = await Client.GetAsync("/catalog/playlists/world_top_100/tracks");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
        client.Dispose();
        wireMockServer.Dispose();

        foreach (var documentId in cleanupDocumentIds.Distinct(StringComparer.Ordinal))
        {
            await EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId);
        }
    }

    private static void ConfigureKworbStub(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/ww/").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(
                        """
                        <html>
                        <body>
                            <table>
                                <tbody>
                                    <tr><td>1</td><td>NEW</td><td>Aurora Lane - Midnight Signals</td></tr>
                                    <tr><td>2</td><td>+3</td><td>Paper Tigers - Static Hearts</td></tr>
                                    <tr><td>3</td><td>-1</td><td>Neon Harbour - Glass Cities</td></tr>
                                    <tr><td>4</td><td>NEW</td><td>Saturn Kids - Golden Echo</td></tr>
                                </tbody>
                            </table>
                        </body>
                        </html>
                        """));
    }

    private static void ConfigureMusicbrainzStub(WireMockServer server)
    {
        server.Given(Request.Create().WithPath("/ws/2/artist").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"artists":[]}"""));

        server.Given(Request.Create().WithPath("/ws/2/release").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"releases":[]}"""));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Midnight Signals Aurora Lane")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-midnight-signals-original",
                              "title": "Midnight Signals",
                              "length": 214000,
                              "releases": [{ "id": "release-midnight-signals", "title": "Midnight Signals", "date": "2023-11-10" }],
                              "artist-credit": [{ "name": "Aurora Lane", "artist": { "id": "musicbrainz-artist:aurora-lane" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Static Hearts Paper Tigers")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-static-hearts-2022",
                              "title": "Static Hearts",
                              "length": 198000,
                              "first-release-date": "2022-09-16",
                              "releases": [{ "id": "release-static-hearts", "title": "Static Hearts", "date": "2022-09-16" }],
                              "artist-credit": [{ "name": "Paper Tigers", "artist": { "id": "musicbrainz-artist:paper-tigers" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Glass Cities Neon Harbour")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-glass-cities-radio-2024",
                              "title": "Glass Cities (Radio Edit)",
                              "length": 231000,
                              "first-release-date": "2024-06-23",
                              "releases": [{ "id": "release-glass-cities-radio", "title": "Glass Cities Remixes", "date": "2024-06-23" }],
                              "artist-credit": [{ "name": "Neon Harbour", "artist": { "id": "musicbrainz-artist:neon-harbour" } }]
                            }
                          ]
                        }
                        """));

        server.Given(
                Request.Create()
                    .WithPath("/ws/2/recording")
                    .WithParam("query", "Golden Echo Saturn Kids")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(
                        """
                        {
                          "recordings": [
                            {
                              "id": "mbid-golden-echo-radio",
                              "title": "Golden Echo - Radio Edit",
                              "length": 244000,
                              "releases": [{ "id": "release-golden-echo-radio", "title": "Golden Echo Radio Release", "date": "2024-02-14" }],
                              "artist-credit": [{ "name": "Saturn Kids", "artist": { "id": "musicbrainz-artist:saturn-kids" } }]
                            }
                          ]
                        }
                        """));
    }

    private static void ConfigureOdesliStub(WireMockServer server)
    {
        server.Given(
                Request.Create()
                    .WithPath("/v1-user/links")
                    .WithParam("artistName", "Aurora Lane")
                    .WithParam("songName", "Midnight Signals")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"linksByPlatform":{"spotify":{"url":"https://open.spotify.com/track/midnight-signals"}}}"""));

        server.Given(
                Request.Create()
                    .WithPath("/v1-user/links")
                    .WithParam("artistName", "Paper Tigers")
                    .WithParam("songName", "Static Hearts")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"linksByPlatform":{}}"""));

        server.Given(
                Request.Create()
                    .WithPath("/v1-user/links")
                    .WithParam("artistName", "Neon Harbour")
                    .WithParam("songName", "Glass Cities (Radio Edit)")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"linksByPlatform":{"youtubeMusic":{"url":"https://music.youtube.com/watch?v=glass-cities-radio"}}}"""));

        server.Given(
                Request.Create()
                    .WithPath("/v1-user/links")
                    .WithParam("artistName", "Saturn Kids")
                    .WithParam("songName", "Golden Echo - Radio Edit")
                    .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody("""{"linksByPlatform":{}}"""));
    }

    private HttpClient CreateExternalClient() =>
        new()
        {
            BaseAddress = new Uri(wireMockServer.Url!, UriKind.Absolute)
        };

    private void TrackForCleanup(string documentId)
    {
        if (!cleanupDocumentIds.Contains(documentId, StringComparer.Ordinal))
        {
            cleanupDocumentIds.Add(documentId);
        }
    }

    internal sealed record StreamingCoverageSummary(IReadOnlyDictionary<string, bool> ByTrackId);

    internal sealed class CommandBusFake : ICommandBus
    {
        public List<IMessage> Messages { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    internal sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
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
                            track.StreamingLocations
                                .Select(static location => new StreamingLocationResponseDto(
                                    location.Provider,
                                    location.ExternalId,
                                    location.Url))
                                .ToArray()))
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

        public TDomain ToDomainObject<TDomain>(object dto) where TDomain : class => (ToDomainObject(dto) as TDomain)!;

        public object ToDomainObject(object? dto)
        {
            var record = (CatalogPlaylistTracksRecordDto)dto!;
            return new GetTracksForPlaylistResponse(
                PlaylistId.FromPlaylistName(record.PlaylistId),
                    record.Tracks.Select(
                        track => new GetTracksForPlaylistTrackResponse(
                            TrackId.From(track.TrackId),
                            track.Title,
                            track.ArtistName,
                            track.AlbumTitle,
                            track.DurationMs,
                            track.Isrc,
                            track.ReleaseDate,
                            track.ArtworkUrl,
                            track.StreamingLocations.Length > 0,
                            track.StreamingLocations
                                .Select(static location => new Soundtrail.Services.Api.Features.Catalog.Shared.Contract.StreamingLocationResponse(
                                    location.Provider,
                                    location.ExternalId,
                                    location.Url))
                                .ToArray()))
                    .ToArray());
        }

        public void MapOnto<TSource, TTarget>(TSource source, TTarget target)
            where TSource : class
            where TTarget : class => throw new NotSupportedException();
    }
}
