using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.ListTracksByAlbum.Adapters;
using Soundtrail.Services.Api.Features.ListTracksByArtist.Adapters;
using Soundtrail.Services.Api.Features.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.ImportMusicTrackEvents;
using Soundtrail.Services.Tests.EndToEnd.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using System.Net.Http.Json;

namespace Soundtrail.Services.Tests.EndToEnd.CatalogBrowsing;

public sealed class CatalogBrowsingOutsideInTestEnvironment : IAsyncDisposable
{
    private readonly WebApplication app;
    private readonly HttpClient client;
    private readonly RavenEmbeddedTestDatabase raven;

    private CatalogBrowsingOutsideInTestEnvironment(
        WebApplication app,
        HttpClient client,
        RavenEmbeddedTestDatabase raven)
    {
        this.app = app;
        this.client = client;
        this.raven = raven;
    }

    public static async Task<CatalogBrowsingOutsideInTestEnvironment> CreateAsync(Action<IDocumentStore> seed)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        seed(raven.Store);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddEmbeddedRavenForTesting(raven.Store);
        builder.Services.AddApiAppServices(builder.Configuration, builder.Environment, options =>
        {
            options.ConfigureCatalogReadDependencies = services =>
            {
                services.AddEmbeddedRavenForTesting(raven.Store);
                services.AddSingleton<Soundtrail.Domain.CatalogBrowsing.ICatalogReadPort, RavenCatalogReadPort>();
            };
        });

        var app = builder.Build();
        app.MapSearchCatalogEndpoints();
        app.MapGetArtistEndpoints();
        app.MapListTracksByArtistEndpoints();
        app.MapGetAlbumEndpoints();
        app.MapListTracksByAlbumEndpoints();
        app.MapGetTrackEndpoints();
        await app.StartAsync();

        return new CatalogBrowsingOutsideInTestEnvironment(app, app.GetTestClient(), raven);
    }

    public async Task<ArtistResponseContract> GetArtistAsync(string artistId) =>
        await client.GetFromJsonAsync<ArtistResponseContract>($"/artists/{artistId}")
        ?? throw new InvalidOperationException("Artist response was not captured.");

    public async Task<ArtistTracksResponseContract> ListTracksByArtistAsync(string artistId) =>
        await client.GetFromJsonAsync<ArtistTracksResponseContract>($"/artists/{artistId}/tracks")
        ?? throw new InvalidOperationException("Artist tracks response was not captured.");

    public async Task<AlbumResponseContract> GetAlbumAsync(string artistId, string albumId) =>
        await client.GetFromJsonAsync<AlbumResponseContract>($"/artists/{artistId}/albums/{albumId}")
        ?? throw new InvalidOperationException("Album response was not captured.");

    public async Task<AlbumTracksResponseContract> ListTracksByAlbumAsync(string artistId, string albumId) =>
        await client.GetFromJsonAsync<AlbumTracksResponseContract>($"/artists/{artistId}/albums/{albumId}/tracks")
        ?? throw new InvalidOperationException("Album tracks response was not captured.");

    public async Task<TrackResponseContract> GetTrackAsync(string artistId, string albumId, string trackId) =>
        await client.GetFromJsonAsync<TrackResponseContract>($"/artists/{artistId}/albums/{albumId}/tracks/{trackId}")
        ?? throw new InvalidOperationException("Track response was not captured.");

    public Task<HttpResponseMessage> GetTrackRawAsync(string artistId, string albumId, string trackId) =>
        client.GetAsync($"/artists/{artistId}/albums/{albumId}/tracks/{trackId}");

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
        raven.Dispose();
    }

    public static void SeedCatalog(
        IDocumentStore store,
        params CatalogSeedTrack[] tracks)
    {
        using var session = store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        foreach (var artist in tracks
                     .GroupBy(x => new { x.ArtistId, x.ArtistName })
                     .Select(group => group.First()))
        {
            session.Store(new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(artist.ArtistId),
                ArtistId = artist.ArtistId,
                Name = artist.ArtistName,
                NormalizedName = artist.ArtistName.ToLowerInvariant(),
                AvailableProviders = artist.AvailableProviders.Select(x => x.Value).ToArray(),
                TerminallyUnavailableProviders = artist.TerminallyUnavailableProviders.Select(x => x.Value).ToArray(),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        foreach (var album in tracks
                     .GroupBy(x => new { x.ArtistId, x.ArtistName, x.AlbumId, x.AlbumName, x.ReleaseDate })
                     .Select(group => group.First()))
        {
            session.Store(new CatalogAlbumRecordDto
            {
                Id = CatalogAlbumRecordDto.GetDocumentId(album.AlbumId),
                ArtistId = album.ArtistId,
                AlbumId = album.AlbumId,
                Name = album.AlbumName,
                NormalizedName = album.AlbumName.ToLowerInvariant(),
                ArtistName = album.ArtistName,
                AvailableProviders = album.AvailableProviders.Select(x => x.Value).ToArray(),
                TerminallyUnavailableProviders = album.TerminallyUnavailableProviders.Select(x => x.Value).ToArray(),
                ReleaseDate = album.ReleaseDate,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        foreach (var track in tracks)
        {
            session.Store(new CatalogTrackRecordDto
            {
                Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId),
                ArtistId = track.ArtistId,
                AlbumId = track.AlbumId,
                TrackId = track.TrackId,
                Title = track.TrackTitle,
                NormalizedTitle = track.TrackTitle.ToLowerInvariant(),
                ArtistName = track.ArtistName,
                AlbumName = track.AlbumName,
                SearchText = $"{track.TrackTitle} {track.ArtistName}".ToLowerInvariant(),
                Isrc = track.Isrc,
                DurationMs = track.DurationMs,
                AvailableProviders = track.AvailableProviders.Select(x => x.Value).ToArray(),
                TerminallyUnavailableProviders = track.TerminallyUnavailableProviders.Select(x => x.Value).ToArray(),
                ProviderReferences = (track.ProviderReferences ?? [])
                    .Select(reference => new CatalogProviderReferenceRecordDto
                    {
                        Provider = ToProviderName(reference.Provider).Value,
                        ProviderEntityType = reference.ProviderEntityType,
                        ProviderId = reference.ProviderId,
                        Url = reference.Url.ToString(),
                        DiscoveredAt = reference.DiscoveredAt
                    })
                    .ToArray(),
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        session.SaveChanges();
    }

    public static void SeedRebuiltCatalogFromImportedEvents(
        IDocumentStore store,
        MusicCatalogId musicCatalogId,
        params IMusicTrackEvent[] events)
    {
        using (var session = store.OpenAsyncSession())
        {
            var importHandler = new ImportMusicTrackEventsHandler(new RavenMusicTrackStreamStore(session));
            importHandler.Handle(
                    new ImportMusicTrackEventsCommand(
                        musicCatalogId,
                        0,
                        CommandId.For($"ImportMusicTrackEvents:{musicCatalogId.Value}"),
                        events),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            session.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        using var replaySession = store.OpenAsyncSession();
        var eventsToReplay = replaySession.Advanced.LoadStartingWithAsync<MusicTrackStoredEventRecordDto>(
                $"music-track-events/{musicCatalogId.Value}/")
            .GetAwaiter()
            .GetResult()
            .OrderBy(x => x.Version)
            .Select(x => new VersionedMusicTrackEvent(x.Version, x.ToDomainEvent()))
            .ToArray();
        var projectHandler = new ProjectMusicTrackCatalogHandler(
            new RavenLoadMusicTrackCatalogProjection(replaySession, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(replaySession, new RavenMusicTrackCatalogProjectionMapper()));
        projectHandler.Handle(
                new ProjectMusicTrackCatalogCommand(musicCatalogId, eventsToReplay),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public sealed record CatalogSeedTrack(
        string ArtistId,
        string ArtistName,
        string AlbumId,
        string AlbumName,
        string TrackId,
        string TrackTitle,
        string? Isrc,
        int? DurationMs,
        DateOnly? ReleaseDate,
        IReadOnlyList<ProviderName> AvailableProviders,
        IReadOnlyList<ProviderName> TerminallyUnavailableProviders,
        IReadOnlyList<ProviderReferenceContract>? ProviderReferences = null);

    public sealed class ArtistResponseContract
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public List<AlbumSummaryContract> Albums { get; set; } = [];
    }

    public sealed class ArtistTracksResponseContract
    {
        public string ArtistId { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public List<TrackSummaryContract> Tracks { get; set; } = [];
    }

    public sealed class AlbumResponseContract
    {
        public string ArtistId { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateOnly? ReleaseDate { get; set; }

        public List<TrackSummaryContract> Tracks { get; set; } = [];
    }

    public sealed class AlbumTracksResponseContract
    {
        public string ArtistId { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string AlbumId { get; set; } = string.Empty;

        public string AlbumName { get; set; } = string.Empty;

        public List<TrackSummaryContract> Tracks { get; set; } = [];
    }

    public sealed class TrackResponseContract
    {
        public string ArtistId { get; set; } = string.Empty;

        public string ArtistName { get; set; } = string.Empty;

        public string AlbumId { get; set; } = string.Empty;

        public string AlbumName { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public int? DurationMs { get; set; }

        public string PlayabilityStatus { get; set; } = string.Empty;

        public List<string> AvailableProviders { get; set; } = [];

        public List<string> TerminallyUnavailableProviders { get; set; } = [];

        public List<ProviderReferenceContract> ProviderReferences { get; set; } = [];
    }

    public sealed class AlbumSummaryContract
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public DateOnly? ReleaseDate { get; set; }

        public string PlayabilityStatus { get; set; } = string.Empty;

        public List<string> AvailableProviders { get; set; } = [];

        public List<string> TerminallyUnavailableProviders { get; set; } = [];
    }

    public sealed class TrackSummaryContract
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AlbumId { get; set; } = string.Empty;

        public string AlbumName { get; set; } = string.Empty;

        public string? Isrc { get; set; }

        public int? DurationMs { get; set; }

        public string PlayabilityStatus { get; set; } = string.Empty;

        public List<string> AvailableProviders { get; set; } = [];

        public List<string> TerminallyUnavailableProviders { get; set; } = [];

        public List<ProviderReferenceContract> ProviderReferences { get; set; } = [];
    }

    public sealed class ProviderReferenceContract
    {
        public string Provider { get; set; } = string.Empty;

        public string ProviderEntityType { get; set; } = string.Empty;

        public string ProviderId { get; set; } = string.Empty;

        public Uri Url { get; set; } = null!;

        public DateTimeOffset DiscoveredAt { get; set; }
    }

    private static ProviderName ToProviderName(string value) =>
        value switch
        {
            "spotify" => ProviderName.Spotify,
            "appleMusic" => ProviderName.AppleMusic,
            "youtubeMusic" => ProviderName.YoutubeMusic,
            _ => ProviderName.From(value)
        };
}
