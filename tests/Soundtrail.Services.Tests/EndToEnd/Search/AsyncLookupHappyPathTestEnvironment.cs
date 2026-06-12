using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Wolverine;
using Wolverine.RavenDb;
using Wolverine.Tracking;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class AsyncLookupHappyPathTestEnvironment : IAsyncDisposable
{
    private readonly IHost host;
    private readonly RavenEmbeddedTestDatabase raven;

    private AsyncLookupHappyPathTestEnvironment(
        IHost host,
        RavenEmbeddedTestDatabase raven)
    {
        this.host = host;
        this.raven = raven;
    }

    public static async Task<AsyncLookupHappyPathTestEnvironment> CreateAsync()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        ExecuteIndexes(raven.Store);

        var metadata = new FakeGetCanonicalMusicMetadata();
        metadata.SeedNames(
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            new SongMetadata(
                "Rare Unknown Song",
                "Test Artist",
                "isrc-rare-1",
                "mbid-rare-1",
                123000));

        var references = new FakeGetMusicTrackReference();
        references.Seed(
            MusicSearchTerm.ByIsrc("isrc-rare-1"),
            new ExternalReference(
                ProviderName.AppleMusic,
                new Uri("https://music.apple.com/track/apple-track-1"),
                "apple-track-1"),
            new ExternalReference(
                ProviderName.YoutubeMusic,
                new Uri("https://music.youtube.com/watch?v=yt-track-1"),
                "yt-track-1"));

        var builder = Host.CreateApplicationBuilder();
        var candidateSearch = new FakeMusicCatalogCandidateSearch();
        candidateSearch.ResolveAs(MusicCatalogId.From("mc_track_1"));
        var localSearch = new LocalMusicTrackSearchFake();
        localSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_1"),
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            Isrc: null,
            Mbid: null,
            DurationMs: null,
            IsPlayable: false));
        builder.Services.RunWolverineInSoloMode();

        builder.Services.AddSingleton<IDocumentStore>(raven.Store);
        builder.Services.AddScoped<IAsyncDocumentSession>(_ => raven.Store.OpenAsyncSession());

        builder.UseWolverine(opts =>
        {
            opts.UseRuntimeCompilation();
            opts.UseRavenDbPersistence();
            opts.Policies.AutoApplyTransactions();
            opts.Durability.DurabilityAgentEnabled = false;
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

        builder.Services.AddSingleton<ITrackSearchPort, RavenTrackSearchIndex>();
        builder.Services.AddSingleton<IMusicCatalogCandidateSearch>(candidateSearch);
        builder.Services.AddSingleton<ILocalMusicTrackSearch>(localSearch);

        builder.Services.AddScoped<IRankedMusicCandidateStore, RavenRankedMusicCandidateStore>();
        builder.Services.AddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        builder.Services.AddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
        builder.Services.AddScoped<IMusicTrackProjectionStore, RavenMusicTrackProjectionStore>();
        builder.Services.AddScoped<IProviderSnapshotStore, RavenProviderSnapshotStore>();

        builder.Services.AddSingleton(metadata);
        builder.Services.AddSingleton<IGetCanonicalMusicMetadata>(metadata);
        builder.Services.AddSingleton(references);
        builder.Services.AddSingleton<IGetMusicTrackReference>(references);
        builder.Services.AddSingleton(new LookupExecutionReceiptStoreFake.State());
        builder.Services.AddSingleton<ILookupExecutionReceiptStore, LookupExecutionReceiptStoreFake>();

        builder.Services.AddSingleton<DiscoveryPriorityPolicy>();
        builder.Services.AddSingleton<MusicCatalogMatchResolver>();

        builder.Services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        builder.Services.AddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();
        builder.Services.AddScoped<LookupMusicRequestHandler>();
        builder.Services.AddScoped<ApplyEnrichmentResponseHandler>();
        builder.Services.AddScoped<OnDemandLookupMetadataHandler>();
        builder.Services.AddScoped<ExecutePlaybackReferencesLookupHandler>();

        builder.Services.AddScoped<LookupMusicRequestListener>();
        builder.Services.AddScoped<MusicBrainzLookupExecutionListener>();
        builder.Services.AddScoped<EnrichmentResponseListener>();
        builder.Services.AddScoped<MusicTrackEventListener>();
        builder.Services.AddScoped<PlaybackReferencesLookupExecutionListener>();
        builder.Services.AddHostedService<MusicTrackEventSubscriptionHostedService>();

        var host = builder.Build();
        await host.StartAsync();

        return new AsyncLookupHappyPathTestEnvironment(host, raven);
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
    }

    private static void ExecuteIndexes(IDocumentStore store)
    {
        IndexCreation.CreateIndexes(typeof(RavenTrackSearchIndex).Assembly, store);
        IndexCreation.CreateIndexes(typeof(RavenMusicCatalogCandidateSearch).Assembly, store);
    }
}