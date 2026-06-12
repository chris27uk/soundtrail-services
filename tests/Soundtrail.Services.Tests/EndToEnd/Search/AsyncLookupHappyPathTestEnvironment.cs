using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Cdc;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker;
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
    private readonly IHost host;
    private readonly RavenEmbeddedTestDatabase raven;
    private readonly WireMockMusicProvidersServer providersServer;

    private AsyncLookupHappyPathTestEnvironment(
        IHost host,
        RavenEmbeddedTestDatabase raven,
        WireMockMusicProvidersServer providersServer)
    {
        this.host = host;
        this.raven = raven;
        this.providersServer = providersServer;
    }

    public static async Task<AsyncLookupHappyPathTestEnvironment> CreateAsync()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteIndexes(raven.Store);
        var providersServer = WireMockMusicProvidersServer.CreateForAsyncLookupHappyPath();

        var builder = Host.CreateApplicationBuilder();
        var candidateSearch = FakeMusicCatalogCandidateSearch.CreateForAsyncLookupHappyPath();
        var localSearch = LocalMusicTrackSearchFake.CreateForAsyncLookupHappyPath();
        var discoveryPlannerDependencies = new EndToEndDiscoveryPlannerDependencyProvider(candidateSearch, localSearch);
        var workerDependencies = new EndToEndWorkerDependencyProvider(providersServer.BaseUrl, new LookupExecutionReceiptStoreFake.State());
        builder.Services.RunWolverineInSoloMode();

        builder.Services.AddEmbeddedRavenForTesting(raven.Store);

        builder.UseWolverine(opts =>
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

            opts.LocalQueueFor<LookupMusicRequestDto>();
            opts.LocalQueueFor<LookupCanonicalMusicMetadataCommandDto>();
            opts.LocalQueueFor<ResolvePlaybackReferencesCommandDto>();
            opts.LocalQueueFor<EnrichmentResponseDto>();
            opts.LocalQueueFor<PlaybackReferencesResolutionRequiredMessageDto>();
        });

        builder.Services.AddApiAppServices(builder.Configuration, builder.Environment, options =>
        {
            options.UseInMemoryQueueing = false;
            options.ConfigureQueueingDependencies = services =>
                services.TryAddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
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
        builder.Services.AddMusicTrackLookupCoordinatorAppServices();

        var host = builder.Build();
        await host.StartAsync();

        return new AsyncLookupHappyPathTestEnvironment(host, raven, providersServer);
    }

    public async Task<SearchMusicResponse> SearchAsync(string query)
    {
        using var scope = this.host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IHandler<SearchMusicRequest, SearchMusicResponse>>();
        return await handler.Handle(new SearchMusicRequest(query, Limit.From(5), MinConfidence: null));
    }

    public async Task<SearchMusicResponse> SearchAndWaitForPipelineAsync(string query, TimeSpan timeout)
    {
        SearchMusicResponse? response = null;
        Func<IMessageContext, Task> executeSearch = async _ =>
        {
            using var scope = this.host.Services.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<IHandler<SearchMusicRequest, SearchMusicResponse>>();
            response = await handler.Handle(new SearchMusicRequest(query, Limit.From(5), MinConfidence: null));
        };

        await this.host.Services
            .TrackActivity(timeout)
            .ExecuteAndWaitAsync(executeSearch);

        return response ?? throw new InvalidOperationException("Search response was not captured.");
    }

    public async Task<SearchMusicResponse> WaitForPlayableSearchAsync(string query, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            var response = await SearchAsync(query);

            if (response.Status == ResolutionStatus.Resolved
                && response.Results.Count > 0
                && response.Results[0].AppleId is not null)
            {
                return response;
            }

            await Task.Delay(25, cts.Token);
        }

        throw new TimeoutException($"Search for '{query}' did not become playable within {timeout}.");
    }

    public async ValueTask DisposeAsync()
    {
        await this.host.StopAsync();
        this.host.Dispose();
        this.raven.Dispose();
        this.providersServer.Dispose();
    }

    private static void ExecuteIndexes(IDocumentStore store)
    {
        IndexCreation.CreateIndexes(typeof(RavenTrackSearchIndex).Assembly, store);
        IndexCreation.CreateIndexes(typeof(RavenMusicCatalogCandidateSearch).Assembly, store);
    }
}
