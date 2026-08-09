using Microsoft.AspNetCore.TestHost;
using Raven.Client.Documents;
using Soundtrail.Adapters.Timing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistApiTestEnvironment : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly IDocumentStore? documentStore;
    private readonly HttpClient client;
    private readonly List<string> cleanupDocumentIds = [];

    private GetTracksForPlaylistApiTestEnvironment(
        WebApplication app,
        IDocumentStore? documentStore,
        HttpClient client,
        CommandBusFake commandBus,
        ClockFake clock,
        PlaylistId playlistId)
    {
        this.app = app;
        this.documentStore = documentStore;
        this.client = client;
        CommandBus = commandBus;
        Clock = clock;
        PlaylistId = playlistId;
    }

    public HttpClient Client => this.client;

    public CommandBusFake CommandBus { get; }

    public ClockFake Clock { get; }

    public PlaylistId PlaylistId { get; }

    public static Task<GetTracksForPlaylistApiTestEnvironment> ForCatchingUpAsync(
        string playlistName = "unknown_playlist") =>
        CreateAsync($"{playlistName}-{EmbeddedRavenTestServer.NewIsolationKey()}");

    public static async Task<GetTracksForPlaylistApiTestEnvironment> ForDiscoveryPresentAsync()
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var playlistName = $"world_top_100-{isolation}";
        var environment = await CreateAsync(playlistName);
        await environment.SeedPlaylistAsync(
            tracks: [],
            discovery: new CatalogDiscoveryFeedbackRecordDto
            {
                TargetId = new CatalogItemOperation.ChildTracksForPlaylist(
                    environment.PlaylistId).StableIdentifier(),
                Status = "scheduled",
                Priority = LookupPriorityBand.High.ToString(),
                NextEligibleAtUtc = DateTimeOffset.UtcNow.AddSeconds(15),
                EarliestExpectedCompletionAtUtc = DateTimeOffset.UtcNow.AddSeconds(75),
                Reason = "Playlist backfill and metadata lookups are still running.",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        return environment;
    }

    public static async Task<GetTracksForPlaylistApiTestEnvironment> ForLookupCompleteAsync()
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var playlistName = $"world_top_100-{isolation}";
        var environment = await CreateAsync(playlistName);
        var trackId = global::Soundtrail.Services.Tests.TestTrackIds.Value($"world-top-100-1-{isolation}");
        await environment.SeedPlaylistAsync(
            tracks:
            [
                new CatalogPlaylistTrackRecordDto
                {
                    TrackId = trackId,
                    MusicCatalogId = trackId,
                    Title = "Midnight Signals",
                    ArtistName = "Aurora Lane",
                    AlbumTitle = "Midnight Signals",
                    DurationMs = 214000,
                    Isrc = null,
                    ReleaseDate = new DateOnly(2023, 11, 10),
                    StreamingLocations =
                    [
                        new CatalogStreamingLocationRecordDto
                        {
                            Provider = "spotify",
                            Url = "https://open.spotify.com/track/midnight-signals"
                        }
                    ]
                }
            ],
            discovery: new CatalogDiscoveryFeedbackRecordDto
            {
                TargetId = new CatalogItemOperation.ChildTracksForPlaylist(
                    environment.PlaylistId).StableIdentifier(),
                Status = "completed",
                Priority = LookupPriorityBand.High.ToString(),
                Reason = "Playlist metadata is available.",
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        return environment;
    }

    public static Task<GetTracksForPlaylistApiTestEnvironment> ForPortFailureAsync(
        string playlistName = "world_top_100") =>
        CreateAsync(
            $"{playlistName}-{EmbeddedRavenTestServer.NewIsolationKey()}",
            portFactory: _ => new FailingGetTracksForPlaylistPort(),
            requireDocumentStore: false);

    public async Task<GetTracksForPlaylistResponseDto?> GetPlaylistAsync()
    {
        var response = await Client.GetAsync($"/catalog/playlists/{PlaylistId.Value}/tracks");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();
    }

    public async ValueTask DisposeAsync()
    {
        await this.app.StopAsync();
        await this.app.DisposeAsync();
        this.client.Dispose();

        if (this.documentStore is not null)
        {
            await EmbeddedRavenTestServer.DeleteDocumentsAsync(this.documentStore, this.cleanupDocumentIds);
            await EmbeddedRavenTestServer.DisposeAsync(this.documentStore);
        }
    }

    public async Task SeedPlaylistAsync(
        CatalogPlaylistTrackRecordDto[] tracks,
        CatalogDiscoveryFeedbackRecordDto? discovery)
    {
        if (this.documentStore is null)
        {
            throw new InvalidOperationException("Document store is required to seed playlist tracks.");
        }

        var now = DateTimeOffset.UtcNow;
        Clock.UtcNow = now;
        var documentId = CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value);
        TrackForCleanup(documentId);

        using var session = this.documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogPlaylistTracksRecordDto
            {
                Id = documentId,
                PlaylistId = PlaylistId.Value,
                TrackIds = tracks.Select(static track => track.TrackId).ToArray(),
                Tracks = tracks,
                Discovery = discovery,
                UpdatedAt = now
            });
        await session.SaveChangesAsync();
    }

    private static async Task<GetTracksForPlaylistApiTestEnvironment> CreateAsync(
        string playlistName,
        Func<IServiceProvider, IGetTracksForPlaylistPort>? portFactory = null,
        bool requireDocumentStore = true)
    {
        IDocumentStore? documentStore = requireDocumentStore
            ? EmbeddedRavenTestServer.CreateDocumentStore()
            : null;
        var commandBus = new CommandBusFake();
        var clock = new ClockFake(DateTimeOffset.UtcNow);
        var playlistId = PlaylistId.FromPlaylistName(playlistName);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        builder.Services.AddSingleton(commandBus);
        builder.Services.AddSingleton<ICommandBus>(sp => sp.GetRequiredService<CommandBusFake>());
        builder.Services.AddSingleton(clock);
        builder.Services.AddSingleton<IClockPort>(sp => sp.GetRequiredService<ClockFake>());
        builder.Services.AddSingleton(AppTypeRegistry.ServiceLocation);
        if (documentStore is not null)
        {
            builder.Services.AddSingleton(documentStore);
        }

        builder.Services.AddSingleton<IGetTracksForPlaylistPort>(sp =>
            portFactory?.Invoke(sp)
            ?? new RavenGetTracksForPlaylistPort(
                sp.GetRequiredService<IDocumentStore>(),
                sp.GetRequiredService<ITypeRegistry>()));
        builder.Services.AddSingleton<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>>(sp =>
            new GetTracksForPlaylistHandler(
                sp.GetRequiredService<IGetTracksForPlaylistPort>(),
                sp.GetRequiredService<ICommandBus>(),
                sp.GetRequiredService<IClockPort>()));

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGetTracksForPlaylistEndpoints(app.Services.GetRequiredService<ITypeRegistry>());
        await app.StartAsync();

        var environment = new GetTracksForPlaylistApiTestEnvironment(
            app,
            documentStore,
            app.GetTestClient(),
            commandBus,
            clock,
            playlistId);
        if (documentStore is not null)
        {
            await environment.ResetScenarioStateAsync();
        }

        return environment;
    }

    private async Task ResetScenarioStateAsync()
    {
        if (this.documentStore is null)
        {
            return;
        }

        var playlistDocumentId = CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value);
        await EmbeddedRavenTestServer.DeleteDocumentAsync(this.documentStore, playlistDocumentId);
    }

    private void TrackForCleanup(string documentId)
    {
        if (!this.cleanupDocumentIds.Contains(documentId, StringComparer.Ordinal))
        {
            this.cleanupDocumentIds.Add(documentId);
        }
    }

    private sealed class FailingGetTracksForPlaylistPort : IGetTracksForPlaylistPort
    {
        public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(
            PlaylistId playlistId,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated playlist tracks port failure.");
    }
}
