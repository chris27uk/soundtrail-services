using JasperFx.CodeGeneration.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Browsing;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Ports;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicTrackEventsImported;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Translators.MusicTrackEventStore;
using System.Net.Http.Json;
using System.Reflection;
using Wolverine;
using Wolverine.Tracking;
using RavenCatalogSearchDiscoveryRepository = Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters.RavenCatalogSearchDiscoveryRepository;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class SearchOutsideInTestEnvironment : IAsyncDisposable
{
    private static readonly IMusicTrackStoredEventRecordTranslator Translator = MusicTrackStoredEventRecordTranslator.Default;
    private readonly WebApplication app;
    private readonly HttpClient client;
    private readonly PipelineMessageCapture pipelineMessageCapture;
    private readonly RavenEmbeddedTestDatabase raven;

    private SearchOutsideInTestEnvironment(
        WebApplication app,
        HttpClient client,
        PipelineMessageCapture pipelineMessageCapture,
        RavenEmbeddedTestDatabase raven)
    {
        this.app = app;
        this.client = client;
        this.pipelineMessageCapture = pipelineMessageCapture;
        this.raven = raven;
    }

    public static async Task<SearchOutsideInTestEnvironment> CreateAsync(Action<IDocumentStore> seed)
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteIndexes(raven.Store);
        seed(raven.Store);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();
        builder.Services.RunWolverineInSoloMode();
        var pipelineMessageCapture = new PipelineMessageCapture();
        builder.Services.AddSingleton(pipelineMessageCapture);
        builder.Services.AddEmbeddedRavenForTesting(raven.Store);

        builder.Host.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();
            opts.Policies.AutoApplyTransactions();
            opts.Durability.DurabilityAgentEnabled = false;
            opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeType<PipelineMessageCaptureHandler>();
            opts.LocalQueueFor<CatalogSearchAttemptDto>();
        });

        builder.Services.AddApiAppServices(builder.Configuration, builder.Environment, options =>
        {
            options.ConfigureQueueingDependencies = services => services.AddCatalogSearchAttemptQueue(builder.Configuration);
            options.ConfigureCatalogSearchDependencies = services =>
            {
                services.AddEmbeddedRavenForTesting(raven.Store);
                services.TryAddSingleton<ICatalogSearchPort, RavenCatalogSearch>();
            };
            options.ConfigureCatalogReadDependencies = services =>
            {
                services.TryAddSingleton<ICatalogReadPort, NoOpCatalogReadPort>();
            };
        });

        var app = builder.Build();
        app.MapSearchCatalogEndpoints();
        await app.StartAsync();

        return new SearchOutsideInTestEnvironment(app, app.GetTestClient(), pipelineMessageCapture, raven);
    }

    public async Task<SearchResponseContract> SearchAndWaitForPipelineAsync(
        string query,
        string? types = null,
        string? playback = null,
        TimeSpan? timeout = null)
    {
        SearchResponseContract? response = null;
        Func<IMessageContext, Task> executeSearch = async _ =>
        {
            response = await SearchAsync(query, types, playback);
        };
        await app.Services.TrackActivity(timeout ?? TimeSpan.FromSeconds(5)).ExecuteAndWaitAsync(executeSearch);

        return response ?? throw new InvalidOperationException("Search response was not captured.");
    }

    public async Task<SearchResponseContract> SearchAsync(
        string query,
        string? types = null,
        string? playback = null)
    {
        var url = $"/search?q={Uri.EscapeDataString(query)}";

        if (!string.IsNullOrWhiteSpace(types))
        {
            url += $"&types={Uri.EscapeDataString(types)}";
        }

        if (!string.IsNullOrWhiteSpace(playback))
        {
            url += $"&playback={Uri.EscapeDataString(playback)}";
        }

        url += "&limit=5";

        return await client.GetFromJsonAsync<SearchResponseContract>(url)
               ?? throw new InvalidOperationException("Search response was not captured.");
    }

    public Task<TMessage> WaitForMessageAsync<TMessage>(TimeSpan timeout) where TMessage : class =>
        pipelineMessageCapture.WaitForAsync<TMessage>(timeout);

    public Task<bool> DidReceiveMessageAsync<TMessage>(TimeSpan timeout) where TMessage : class =>
        pipelineMessageCapture.DidReceiveAsync<TMessage>(timeout);

    public int CountMessages<TMessage>() where TMessage : class =>
        pipelineMessageCapture.Count<TMessage>();

    public async Task<bool> HasDiscoveryRequestAsync(string criteria)
    {
        using var session = raven.Store.OpenAsyncSession();
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(criteria),
            CancellationToken.None);
        return metadata is not null;
    }

    public async Task<int> CountDiscoveryRequestEventsAsync(string criteria)
    {
        using var session = raven.Store.OpenAsyncSession();
        var metadata = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(criteria),
            CancellationToken.None);
        return metadata?.Version ?? 0;
    }

    public async ValueTask DisposeAsync()
    {
        client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
        raven.Dispose();
    }

    public sealed class SearchResponseContract
    {
        public string Query { get; set; } = string.Empty;

        public List<SearchResultContract> Results { get; set; } = [];

        public SearchDiscoveryContract Discovery { get; set; } = new();
    }

    public sealed class SearchResultContract
    {
        public string Type { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ArtistId { get; set; }

        public string? ArtistName { get; set; }

        public string? AlbumId { get; set; }

        public string? AlbumName { get; set; }

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

    public sealed class SearchDiscoveryContract
    {
        public bool WillBeLookedUp { get; set; }

        public string? Reason { get; set; }

        public int? RetryAfterSeconds { get; set; }
    }

    private static void ExecuteIndexes(IDocumentStore store)
    {
        foreach (var type in IndexTypes)
        {
            var index = (AbstractIndexCreationTask)Activator.CreateInstance(type)!;
            index.Execute(store);
        }
    }

    public static void SeedPlayableTrack(
        IDocumentStore store,
        string query,
        string trackId,
        string title,
        string artistId,
        string artistName,
        string albumId,
        string albumName,
        params ProviderName[] availableProviders)
    {
        using var session = store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        var track = Activator.CreateInstance(CatalogTrackRecordDtoType)!;
        Set(track, CatalogTrackRecordDtoType, "Id", $"catalog/tracks/{trackId}");
        Set(track, CatalogTrackRecordDtoType, "ArtistId", artistId);
        Set(track, CatalogTrackRecordDtoType, "AlbumId", albumId);
        Set(track, CatalogTrackRecordDtoType, "TrackId", trackId);
        Set(track, CatalogTrackRecordDtoType, "Title", title);
        Set(track, CatalogTrackRecordDtoType, "NormalizedTitle", title.ToLowerInvariant());
        Set(track, CatalogTrackRecordDtoType, "ArtistName", artistName);
        Set(track, CatalogTrackRecordDtoType, "AlbumName", albumName);
        Set(track, CatalogTrackRecordDtoType, "SearchText", MusicIdentityText.NormalizeFreeText(query));
        Set(track, CatalogTrackRecordDtoType, "MusicBrainzRecordingId", null);
        Set(track, CatalogTrackRecordDtoType, "Isrc", null);
        Set(track, CatalogTrackRecordDtoType, "DurationMs", null);
        Set(track, CatalogTrackRecordDtoType, "AvailableProviders", availableProviders.Select(x => x.Value).ToArray());
        Set(track, CatalogTrackRecordDtoType, "TerminallyUnavailableProviders", Array.Empty<string>());
        Set(track, CatalogTrackRecordDtoType, "ProviderReferences", CreateProviderReferences(availableProviders));
        Set(track, CatalogTrackRecordDtoType, "ArtworkUrl", null);
        Set(track, CatalogTrackRecordDtoType, "UpdatedAt", DateTimeOffset.UtcNow);
        session.Store(track);
        session.SaveChanges();
    }

    public static void SeedProjectedCatalogSearchStatusFromEvents(
        IDocumentStore store,
        MusicSearchCriteria searchCriteria,
        params IDomainEvent[] events)
    {
        var repository = new RavenCatalogSearchDiscoveryRepository(store);
        repository.AppendAsync(searchCriteria, 0, events, CancellationToken.None).GetAwaiter().GetResult();

        using var session = store.OpenAsyncSession();
        var replayHandler = new ReplayCatalogSearchStatusHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(session),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default)));
        replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(searchCriteria),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public static void SeedRebuiltCatalogProjectionFromImportedEvents(
        IDocumentStore store,
        MusicCatalogId musicCatalogId,
        params IMusicTrackEvent[] events)
    {
        using (var session = store.OpenAsyncSession())
        {
            var importHandler = new MusicTrackEventsImportedHandler(new RavenMusicTrackStreamStore(session, Translator));
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
            .Select(x => new VersionedMusicTrackEvent(x.Version, Translator.ToDomainObject(x)))
            .ToArray();
        var projectHandler = new MusicCatalogChangedHandler(
            new RavenLoadMusicTrackCatalogProjection(replaySession, new RavenMusicTrackCatalogProjectionMapper()),
            new RavenSaveMusicTrackCatalogProjection(replaySession, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default));
        projectHandler.Handle(
                new MusicCatalogChangedCommand(musicCatalogId, eventsToReplay),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public static void SeedRebuiltDiscoveryProjectionFromImportedEvents(
        IDocumentStore store,
        MusicSearchCriteria searchCriteria,
        params IDomainEvent[] events)
    {
        using var replaySession = store.OpenAsyncSession();
        var repository = new RavenCatalogSearchDiscoveryRepository(store);
        repository.AppendAsync(searchCriteria, 0, events, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        var replayHandler = new ReplayCatalogSearchStatusHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(replaySession),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(replaySession, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(replaySession, Soundtrail.Translators.Registry.TypeTranslationRegistry.Default)));
        replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(searchCriteria),
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static void Set(object target, Type targetType, string propertyName, object? value) =>
        targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private static Array CreateProviderReferences(IReadOnlyList<ProviderName> providers)
    {
        var array = Array.CreateInstance(CatalogProviderReferenceRecordDtoType, providers.Count);

        for (var index = 0; index < providers.Count; index++)
        {
            var reference = Activator.CreateInstance(CatalogProviderReferenceRecordDtoType)!;
            Set(reference, CatalogProviderReferenceRecordDtoType, "Provider", providers[index].Value);
            Set(reference, CatalogProviderReferenceRecordDtoType, "ProviderEntityType", "track");
            Set(reference, CatalogProviderReferenceRecordDtoType, "ProviderId", $"{providers[index].Value.ToLowerInvariant()}-{index + 1}");
            Set(reference, CatalogProviderReferenceRecordDtoType, "Url", $"https://example.com/{providers[index].Value.ToLowerInvariant()}/{index + 1}");
            Set(reference, CatalogProviderReferenceRecordDtoType, "DiscoveredAt", DateTimeOffset.UtcNow);
            array.SetValue(reference, index);
        }

        return array;
    }

    private sealed class NoOpCatalogReadPort : ICatalogReadPort
    {
        public Task<ArtistDetailsResponse?> GetArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
            Task.FromResult<ArtistDetailsResponse?>(null);

        public Task<IReadOnlyList<TrackSummary>> ListTracksByArtistAsync(ArtistId artistId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TrackSummary>>([]);

        public Task<AlbumDetailsResponse?> GetAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
            Task.FromResult<AlbumDetailsResponse?>(null);

        public Task<AlbumTracksResponse?> ListTracksByAlbumAsync(ArtistId artistId, AlbumId albumId, CancellationToken cancellationToken) =>
            Task.FromResult<AlbumTracksResponse?>(null);

        public Task<TrackDetailsResponse?> GetTrackAsync(ArtistId artistId, AlbumId albumId, TrackId trackId, CancellationToken cancellationToken) =>
            Task.FromResult<TrackDetailsResponse?>(null);
    }

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;

    private static readonly Type CatalogTrackRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogTrackRecordDto);

    private static readonly Type CatalogProviderReferenceRecordDtoType = typeof(Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogProviderReferenceRecordDto);

    private static readonly IReadOnlyList<Type> IndexTypes =
    [
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Artists", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Albums", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Tracks", true)!
    ];
}
