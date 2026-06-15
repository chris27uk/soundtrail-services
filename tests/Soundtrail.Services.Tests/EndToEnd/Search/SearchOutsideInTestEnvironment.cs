using JasperFx.CodeGeneration.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.Search.SearchCatalog;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Features.Search.SearchCatalog.Ports;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Wolverine;
using Wolverine.Tracking;
using System.Net.Http.Json;
using System.Reflection;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class SearchOutsideInTestEnvironment : IAsyncDisposable
{
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
            options.UseInMemoryQueueing = false;
            options.ConfigureQueueingDependencies = services => services.TryAddScoped<IEnqueueCatalogSearchAttempt, WolverineEnqueueCatalogSearchAttempt>();
            options.ConfigureCatalogSearchDependencies = services =>
            {
                services.AddEmbeddedRavenForTesting(raven.Store);
                services.TryAddSingleton<Soundtrail.Domain.Search.ICatalogSearchPort, Soundtrail.Services.Api.Infrastructure.Raven.RavenCatalogSearch>();
            };
            options.ConfigureCatalogReadDependencies = services =>
            {
                services.TryAddSingleton<Soundtrail.Domain.CatalogBrowsing.ICatalogReadPort, NoOpCatalogReadPort>();
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
        Func<Wolverine.IMessageContext, Task> executeSearch = async _ =>
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
        var events = await session.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .WhereEquals(nameof(DiscoveryQueryStoredEventRecordDto.Criteria), criteria)
            .ToListAsync(CancellationToken.None);
        return events.Count;
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
        Set(track, CatalogTrackRecordDtoType, "SearchText", NormalizedSearchQuery.FromText(query).Value);
        Set(track, CatalogTrackRecordDtoType, "MusicBrainzRecordingId", null);
        Set(track, CatalogTrackRecordDtoType, "Isrc", null);
        Set(track, CatalogTrackRecordDtoType, "DurationMs", null);
        Set(track, CatalogTrackRecordDtoType, "AvailableProviders", availableProviders.Select(x => x.Value).ToArray());
        Set(track, CatalogTrackRecordDtoType, "TerminallyUnavailableProviders", Array.Empty<string>());
        Set(track, CatalogTrackRecordDtoType, "ArtworkUrl", null);
        Set(track, CatalogTrackRecordDtoType, "UpdatedAt", DateTimeOffset.UtcNow);
        session.Store(track);
        session.SaveChanges();
    }

    public static void SeedCatalogSearchStatus(
        IDocumentStore store,
        string normalizedQuery,
        string types,
        CatalogSearchLifecycleStatus status,
        bool willBeLookedUp,
        string reason,
        int? retryAfterSeconds)
    {
        using var session = store.OpenSession();
        session.Advanced.WaitForIndexesAfterSaveChanges();

        var criteria = CatalogSearchCriteria.Search(types, normalizedQuery).Value;
        var document = new CatalogSearchStatusRecordDto
        {
            Id = CatalogSearchStatusRecordDto.GetDocumentId(criteria),
            Criteria = criteria,
            Status = status.ToString(),
            Priority = "High",
            WillBeLookedUp = willBeLookedUp,
            EstimatedRetryAfterSeconds = retryAfterSeconds,
            EarliestExpectedCompletionAt = null,
            Reason = reason,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        session.Store(document);
        session.SaveChanges();
    }

    private static void Set(object target, Type targetType, string propertyName, object? value) =>
        targetType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(target, value);

    private sealed class NoOpCatalogReadPort : Soundtrail.Domain.CatalogBrowsing.ICatalogReadPort
    {
        public Task<Soundtrail.Domain.CatalogBrowsing.ArtistDetailsResponse?> GetArtistAsync(Soundtrail.Domain.Catalog.ArtistId artistId, CancellationToken cancellationToken) =>
            Task.FromResult<Soundtrail.Domain.CatalogBrowsing.ArtistDetailsResponse?>(null);

        public Task<IReadOnlyList<Soundtrail.Domain.CatalogBrowsing.TrackSummary>> ListTracksByArtistAsync(Soundtrail.Domain.Catalog.ArtistId artistId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Soundtrail.Domain.CatalogBrowsing.TrackSummary>>([]);

        public Task<Soundtrail.Domain.CatalogBrowsing.AlbumDetailsResponse?> GetAlbumAsync(Soundtrail.Domain.Catalog.ArtistId artistId, Soundtrail.Domain.Catalog.AlbumId albumId, CancellationToken cancellationToken) =>
            Task.FromResult<Soundtrail.Domain.CatalogBrowsing.AlbumDetailsResponse?>(null);

        public Task<Soundtrail.Domain.CatalogBrowsing.AlbumTracksResponse?> ListTracksByAlbumAsync(Soundtrail.Domain.Catalog.ArtistId artistId, Soundtrail.Domain.Catalog.AlbumId albumId, CancellationToken cancellationToken) =>
            Task.FromResult<Soundtrail.Domain.CatalogBrowsing.AlbumTracksResponse?>(null);

        public Task<Soundtrail.Domain.CatalogBrowsing.TrackDetailsResponse?> GetTrackAsync(Soundtrail.Domain.Catalog.ArtistId artistId, Soundtrail.Domain.Catalog.AlbumId albumId, Soundtrail.Domain.Catalog.TrackId trackId, CancellationToken cancellationToken) =>
            Task.FromResult<Soundtrail.Domain.CatalogBrowsing.TrackDetailsResponse?>(null);
    }

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;

    private static readonly Type CatalogTrackRecordDtoType = ApiAssembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogTrackRecordDto", true)!;

    private static readonly IReadOnlyList<Type> IndexTypes =
    [
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Artists", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Albums", true)!,
        ApiAssembly.GetType("Soundtrail.Services.Api.Infrastructure.Raven.Indexes.Search_Tracks", true)!
    ];
}
