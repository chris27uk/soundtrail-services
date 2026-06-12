using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.LocalSearch;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

public sealed class AsyncLookupHappyPathTests
{
    [Fact]
    public async Task Given_A_Missing_Search_Result_When_The_Local_Wolverine_Pipeline_Completes_Then_A_Requery_Returns_The_Track()
    {
        await using var env = await AsyncLookupHappyPathTestEnvironment.CreateAsync();

        var first = await env.SearchAsync("rare unknown song");

        first.Status.Should().Be(ResolutionStatus.Pending);
        first.Results.Should().BeEmpty();

        var resolved = await env.WaitForResolvedSearchAsync("rare unknown song", TimeSpan.FromSeconds(5));

        resolved.Status.Should().Be(ResolutionStatus.Resolved);
        resolved.Results.Should().ContainSingle();
        resolved.Results[0].Title.Value.Should().Be("Rare Unknown Song");
        resolved.Results[0].Artist.Value.Should().Be("Test Artist");
        resolved.Results[0].AppleId!.Value.Should().Be("apple-track-1");
    }

    private sealed class AsyncLookupHappyPathTestEnvironment : IAsyncDisposable
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

        public static Task<AsyncLookupHappyPathTestEnvironment> CreateAsync()
        {
            var raven = RavenEmbeddedTestDatabase.Create();
            SeedSeedTrack(raven.Store);

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

            builder.Services.AddSingleton<IDocumentStore>(raven.Store);
            builder.Services.AddScoped<IAsyncDocumentSession>(_ => raven.Store.OpenAsyncSession());

            builder.Services.AddSingleton<IMusicCatalogCandidateSearch>(new PlannerProjectionCandidateSearch(raven.Store));
            builder.Services.AddSingleton<ILocalMusicTrackSearch>(new RavenLocalMusicTrackSearch(raven.Store));
            builder.Services.AddSingleton<ITrackSearchPort>(new PlannerProjectionTrackSearchPort(raven.Store));

            builder.Services.AddScoped<IRankedMusicCandidateStore, RavenRankedMusicCandidateStore>();
            builder.Services.AddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
            builder.Services.AddScoped<IMusicTrackEventRepository, RavenMusicTrackStreamStore>();
            builder.Services.AddScoped<IMusicTrackProjectionStore, RavenMusicTrackProjectionStore>();
            builder.Services.AddScoped<IProviderSnapshotStore, RavenProviderSnapshotStore>();

            builder.Services.AddSingleton(metadata);
            builder.Services.AddSingleton<IGetCanonicalMusicMetadata>(metadata);
            builder.Services.AddSingleton(references);
            builder.Services.AddSingleton<IGetMusicTrackReference>(references);
            builder.Services.AddSingleton<ILookupExecutionReceiptStore, InMemoryLookupExecutionReceiptStore>();

            builder.Services.AddSingleton<DiscoveryPriorityPolicy>();
            builder.Services.AddSingleton<MusicCatalogMatchResolver>();

            builder.Services.AddScoped<IEnqueueMusicRequest, InProcessEnqueueMusicRequest>();
            builder.Services.AddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();
            builder.Services.AddScoped<LookupMusicRequestHandler>();
            builder.Services.AddScoped<ApplyEnrichmentResponseHandler>();
            builder.Services.AddScoped<OnDemandLookupMetadataHandler>();
            builder.Services.AddScoped<ExecutePlaybackReferencesLookupHandler>();

            builder.Services.AddScoped<LookupMusicRequestListener>();
            builder.Services.AddScoped<MusicBrainzLookupExecutionListener>();
            builder.Services.AddScoped<EnrichmentResponseBridge>();
            builder.Services.AddScoped<MusicTrackEventListener>();
            builder.Services.AddScoped<PlaybackReferencesLookupExecutionListener>();

            var host = builder.Build();

            return Task.FromResult(new AsyncLookupHappyPathTestEnvironment(host, raven));
        }

        public async Task<SearchMusicResponse> SearchAsync(string query)
        {
            using var scope = host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<SearchMusicRequest, SearchMusicResponse>>();
            return await handler.Handle(new SearchMusicRequest(query, Limit.From(5), MinConfidence: null));
        }

        public async Task<SearchMusicResponse> WaitForResolvedSearchAsync(string query, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            while (!cts.IsCancellationRequested)
            {
                var response = await SearchAsync(query);
                if (response.Status == ResolutionStatus.Resolved)
                {
                    return response;
                }

                await Task.Delay(25, cts.Token);
            }

            throw new TimeoutException($"Search for '{query}' did not resolve within {timeout}.");
        }

        public async ValueTask DisposeAsync()
        {
            await host.StopAsync();
            host.Dispose();
            raven.Dispose();
        }

        private static void SeedSeedTrack(IDocumentStore store)
        {
            using var session = store.OpenSession();
            session.Advanced.WaitForIndexesAfterSaveChanges();
            session.Store(new RavenTrackDocument
            {
                Id = RavenTrackDocument.GetDocumentId("mc_track_1"),
                Title = "Rare Unknown Song",
                Artist = "Test Artist",
                AlbumTitle = "Rare Album",
                SearchText = RavenTrackDocument.BuildSearchText("Rare Unknown Song", "Test Artist"),
                IsPlayable = false
            });
            session.SaveChanges();
        }
    }

    public sealed class EnrichmentResponseBridge(
        ApplyEnrichmentResponseHandler handler,
        MusicTrackEventListener musicTrackEventListener)
    {
        public async Task<object[]> Handle(
            EnrichmentResponseDto dto,
            IAsyncDocumentSession session,
            CancellationToken cancellationToken)
        {
            var result = await handler.Handle(
                new EnrichmentResponse(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    ProviderName.From(dto.SourceProvider),
                    dto.Priority,
                    dto.CreatedAt,
                    dto.Metadata is null
                        ? null
                        : new SongMetadata(
                            dto.Metadata.Title,
                            dto.Metadata.Artist,
                            dto.Metadata.Isrc,
                            dto.Metadata.Mbid,
                            dto.Metadata.DurationMs),
                    dto.References.Select(reference => new ExternalReference(
                        ProviderName.From(reference.Provider),
                        reference.Url,
                        reference.ExternalId)).ToArray(),
                    CorrelationId.From(dto.CorrelationId)),
                cancellationToken);

            return result.Events
                .OfType<Soundtrail.Domain.Events.PlaybackReferencesResolutionRequired>()
                .Select(playback => new PlaybackReferencesResolutionRequiredMessageDto(
                    playback.MusicCatalogId.Value,
                    playback.Priority,
                    playback.CorrelationId.Value,
                    playback.SourceProvider.Value,
                    playback.ObservedAt,
                    new PlaybackReferenceSearchTermDto(
                        playback.SearchTerm.Isrc,
                        playback.SearchTerm.Title,
                        playback.SearchTerm.Artist,
                        playback.SearchTerm.Album)))
                .Select(message => musicTrackEventListener.Handle(message, session))
                .Cast<object>()
                .ToArray();
        }
    }

    private sealed class InProcessEnqueueMusicRequest(
        IAsyncDocumentSession session,
        LookupMusicRequestListener lookupMusicRequestListener,
        MusicBrainzLookupExecutionListener musicBrainzLookupExecutionListener,
        PlaybackReferencesLookupExecutionListener playbackReferencesLookupExecutionListener,
        EnrichmentResponseBridge enrichmentResponseBridge) : IEnqueueMusicRequest
    {
        public async Task EnqueueAsync(
            Soundtrail.Services.Api.Features.Search.Queueing.LookupMusicRequest request,
            CancellationToken cancellationToken)
        {
            var pending = new Queue<object>(await lookupMusicRequestListener.Handle(
                new LookupMusicRequestDto(
                    request.Query,
                    request.TrustLevel,
                    request.RiskScore,
                    request.OccurredAt,
                    request.CorrelationId),
                session,
                cancellationToken));

            while (pending.Count > 0)
            {
                var message = pending.Dequeue();
                switch (message)
                {
                    case LookupCanonicalMusicMetadataCommandDto canonical:
                        foreach (var response in await musicBrainzLookupExecutionListener.Handle(canonical, session, cancellationToken))
                        {
                            pending.Enqueue(response);
                        }
                        break;
                    case ResolvePlaybackReferencesCommandDto playback:
                        foreach (var response in await playbackReferencesLookupExecutionListener.Handle(playback, session, cancellationToken))
                        {
                            pending.Enqueue(response);
                        }
                        break;
                    case EnrichmentResponseDto response:
                        foreach (var next in await enrichmentResponseBridge.Handle(response, session, cancellationToken))
                        {
                            pending.Enqueue(next);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(message), message, null);
                }
            }

            await session.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class PlannerProjectionTrackSearchPort(IDocumentStore documentStore) : ITrackSearchPort
    {
        public async Task<IReadOnlyList<SearchResult>> SearchAsync(
            Soundtrail.Services.Api.Features.Search.TrackSearch.NormalizedSearchQuery query,
            Limit limit,
            CancellationToken cancellationToken)
        {
            using var session = documentStore.OpenAsyncSession();
            var documents = await session.Query<RavenTrackDocument>()
                .Take(32)
                .ToListAsync(cancellationToken);

            return documents
                .Where(document => document.IsPlayable)
                .Where(document =>
                    Soundtrail.Services.Api.Features.Search.TrackSearch.NormalizedSearchQuery.FromText(
                        $"{document.Title} {document.Artist}").Value.Contains(query.Value, StringComparison.Ordinal))
                .Take(limit.Value)
                .Select(document => new SearchResult(
                    TrackTitle.From(document.Title),
                    ArtistName.From(document.Artist),
                    string.IsNullOrWhiteSpace(document.Isrc) ? null : Isrc.From(document.Isrc),
                    string.IsNullOrWhiteSpace(document.Mbid) ? null : Mbid.From(document.Mbid),
                    string.IsNullOrWhiteSpace(document.AppleId) ? null : AppleId.From(document.AppleId),
                    string.IsNullOrWhiteSpace(document.SpotifyId) ? null : SpotifyId.From(document.SpotifyId),
                    ConfidenceScore.From(0.95)))
                .ToArray();
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class PlannerProjectionCandidateSearch(IDocumentStore documentStore) : IMusicCatalogCandidateSearch
    {
        public async Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
            Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.NormalizedSearchQuery query,
            CancellationToken cancellationToken)
        {
            using var session = documentStore.OpenAsyncSession();
            var documents = await session.Query<RavenTrackDocument>()
                .Take(32)
                .ToListAsync(cancellationToken);

            return documents
                .Where(document => document.SearchText.Contains(query.Value, StringComparison.Ordinal))
                .Select(document => new MusicCatalogMatch(
                    MusicCatalogId.From(document.Id.Replace("track-catalogue/", string.Empty)),
                    0.95m))
                .ToArray();
        }
    }

    private sealed class InMemoryLookupExecutionReceiptStore : ILookupExecutionReceiptStore
    {
        private readonly HashSet<string> commandIds = [];
        private readonly HashSet<string> completed = [];

        public Task<bool> TryBeginAsync(CommandId commandId, CancellationToken cancellationToken) =>
            Task.FromResult(commandIds.Add(commandId.Value));

        public Task MarkCompletedAsync(CommandId commandId, CancellationToken cancellationToken)
        {
            completed.Add(commandId.Value);
            return Task.CompletedTask;
        }
    }
}
