using JasperFx.CodeGeneration.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;
using System.Text.Json;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Cdc;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.ProviderClients;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Wolverine;
using Wolverine.RavenDb;
using Wolverine.Tracking;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class AsyncLookupHappyPathTestEnvironment : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplication app;
    private readonly HttpClient client;
    private readonly PipelineMessageCapture pipelineMessageCapture;
    private readonly RavenEmbeddedTestDatabase raven;
    private readonly WireMockMusicProvidersServer providersServer;

    private AsyncLookupHappyPathTestEnvironment(
        WebApplication app,
        HttpClient client,
        PipelineMessageCapture pipelineMessageCapture,
        RavenEmbeddedTestDatabase raven,
        WireMockMusicProvidersServer providersServer)
    {
        this.app = app;
        this.client = client;
        this.pipelineMessageCapture = pipelineMessageCapture;
        this.raven = raven;
        this.providersServer = providersServer;
    }

    public static async Task<AsyncLookupHappyPathTestEnvironment> CreateAsync()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        var providersServer = WireMockMusicProvidersServer.CreateForAsyncLookupHappyPath();
        SeedLocalTrack(raven);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        var candidateSearch = FakeMusicCatalogCandidateSearch.CreateForAsyncLookupHappyPath();
        var discoveryPlannerDependencies = new EndToEndDiscoveryPlannerDependencyProvider(candidateSearch);
        var workerDependencies = new EndToEndWorkerDependencyProvider(providersServer.BaseUrl, new LookupExecutionReceiptStoreFake.State());
        var pipelineMessageCapture = new PipelineMessageCapture();
        builder.Services.RunWolverineInSoloMode();
        builder.Services.AddSingleton(pipelineMessageCapture);

        builder.Services.AddEmbeddedRavenForTesting(raven.Store);

        builder.Host.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();
            opts.UseRavenDbPersistence();
            opts.Policies.AutoApplyTransactions();
            opts.Durability.DurabilityAgentEnabled = false;
            opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeType<LookupMusicRequestListener>();
            opts.Discovery.IncludeType<MusicBrainzLookupExecutionListener>();
            opts.Discovery.IncludeType<PlaybackReferencesLookupExecutionListener>();
            opts.Discovery.IncludeType<EnrichmentResponseListener>();
            opts.Discovery.IncludeType<MusicTrackEventListener>();
            opts.Discovery.IncludeType<PipelineMessageCaptureHandler>();

            opts.LocalQueueFor<LookupMusicRequestDto>();
            opts.LocalQueueFor<LookupCanonicalMusicMetadataCommandDto>();
            opts.LocalQueueFor<ResolvePlaybackReferencesCommandDto>();
            opts.LocalQueueFor<EnrichmentResponseDto>();
            opts.LocalQueueFor<PlaybackReferencesResolutionRequiredMessageDto>();
        });

        builder.Services.AddApiAppServices(builder.Configuration, builder.Environment, options =>
        {
            options.UseInMemoryQueueing = false;
            options.ConfigureQueueingDependencies = services => services.TryAddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        });
        builder.Services.AddDiscoveryPlannerAppServices(builder.Configuration, options =>
        {
            options.IncludeBacklogHostedService = false;
            options.DependencyProvider = discoveryPlannerDependencies;
        });
        builder.Services.AddWorkerAppServices(
            builder.Configuration,
            options => options.DependencyProvider = workerDependencies);
        builder.Services.AddCdcAppServices(builder.Configuration);
        builder.Services.AddMusicTrackLookupCoordinatorAppServices(builder.Configuration);

        var app = builder.Build();
        app.MapSearchEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();

        return new AsyncLookupHappyPathTestEnvironment(app, client, pipelineMessageCapture, raven, providersServer);
    }

    private static void SeedLocalTrack(RavenEmbeddedTestDatabase raven)
    {
        using var session = raven.Store.OpenSession();
        session.Store(new RavenTrackDocument
        {
            Id = RavenTrackDocument.GetDocumentId("mc_track_1"),
            Title = "Rare Unknown Song",
            Artist = "Test Artist",
            AlbumTitle = "Rare Album",
            SearchText = string.Empty,
            CanonicalMetadata = new RavenSongMetadataDocument
            {
                Title = "Rare Unknown Song",
                Artist = "Test Artist"
            },
            IsPlayable = false
        });
        session.SaveChanges();
    }

    public async Task<SearchHttpResponseDto> SearchAsync(string query)
    {
        using var response = await this.client.GetAsync($"/search?q={Uri.EscapeDataString(query)}&limit=5");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchHttpResponseDto>(JsonOptions)
               ?? throw new InvalidOperationException("Search response was not captured.");
    }

    public async Task<SearchHttpResponseDto> SearchAndWaitForPipelineAsync(string query, TimeSpan timeout)
    {
        SearchHttpResponseDto? response = null;
        Func<IMessageContext, Task> executeSearch = async _ =>
        {
            response = await SearchAsync(query);
        };

        await this.app.Services
            .TrackActivity(timeout)
            .ExecuteAndWaitAsync(executeSearch);

        return response ?? throw new InvalidOperationException("Search response was not captured.");
    }

    public async Task<SearchHttpResponseDto> WaitForPlayableSearchAsync(string query, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            var response = await SearchAsync(query);

            if (string.Equals(response.Status, "resolved", StringComparison.OrdinalIgnoreCase)
                && response.Results.Count > 0
                && response.Results[0].AppleId is not null)
            {
                return response;
            }

            await Task.Delay(25, cts.Token);
        }

        throw new TimeoutException($"Search for '{query}' did not become playable within {timeout}.");
    }

    public Task<TMessage> WaitForMessageAsync<TMessage>(TimeSpan timeout) where TMessage : class =>
        this.pipelineMessageCapture.WaitForAsync<TMessage>(timeout);

    public async ValueTask DisposeAsync()
    {
        this.client.Dispose();
        await this.app.StopAsync();
        await this.app.DisposeAsync();
        this.raven.Dispose();
        this.providersServer.Dispose();
    }
}

public sealed record SearchHttpResponseDto(
    string Status,
    string Query,
    int? RetryAfterSeconds,
    IReadOnlyList<SearchHttpResultDto> Results);

public sealed record SearchHttpResultDto(
    string Title,
    string Artist,
    string? Isrc,
    string? Mbid,
    string? AppleId,
    string? SpotifyId,
    double Confidence);
