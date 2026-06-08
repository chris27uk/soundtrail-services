using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Api;
using Soundtrail.Contracts.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Events;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Api.Features.Search.Tracks;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using Wolverine;
using Wolverine.Attributes;

namespace Soundtrail.Services.Tests.Integration.EndToEnd.Search;

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
    }

    private sealed class AsyncLookupHappyPathTestEnvironment : IAsyncDisposable
    {
        private readonly IHost host;

        private AsyncLookupHappyPathTestEnvironment(
            IHost host,
            FakeMusicCatalogCandidateSearch candidateSearch,
            MusicTrackProjectionStoreFake projectionStore)
        {
            this.host = host;
            CandidateSearch = candidateSearch;
            ProjectionStore = projectionStore;
        }

        public FakeMusicCatalogCandidateSearch CandidateSearch { get; }

        public MusicTrackProjectionStoreFake ProjectionStore { get; }

        public static async Task<AsyncLookupHappyPathTestEnvironment> CreateAsync()
        {
            var builder = Host.CreateApplicationBuilder();

            var candidateSearch = new FakeMusicCatalogCandidateSearch();
            candidateSearch.ResolveAs(MusicCatalogId.From("mc_track_1"));

            var rankedStore = new RankedMusicCandidateStoreFake();
            var activeLookupWorkStore = new ActiveLookupWorkStoreFake();
            var streamStore = new MusicTrackStreamStoreFake();
            var projectionStore = new MusicTrackProjectionStoreFake();
            var snapshotStore = new ProviderSnapshotStoreFake();

            builder.Services.AddSingleton(candidateSearch);
            builder.Services.AddSingleton<IMusicCatalogCandidateSearch>(candidateSearch);
            builder.Services.AddSingleton(rankedStore);
            builder.Services.AddSingleton<IRankedMusicCandidateStore>(rankedStore);
            builder.Services.AddSingleton(activeLookupWorkStore);
            builder.Services.AddSingleton<IActiveLookupWorkStore>(activeLookupWorkStore);
            builder.Services.AddSingleton(streamStore);
            builder.Services.AddSingleton<IMusicTrackEventRepository>(streamStore);
            builder.Services.AddSingleton(projectionStore);
            builder.Services.AddSingleton<IMusicTrackProjectionStore>(projectionStore);
            builder.Services.AddSingleton(snapshotStore);
            builder.Services.AddSingleton<IProviderSnapshotStore>(snapshotStore);
            builder.Services.AddSingleton<ITrackSearchPort, ProjectionBackedTrackSearchPort>();
            builder.Services.AddSingleton<DiscoveryPriorityPolicy>();
            builder.Services.AddSingleton<MusicCatalogResolutionPolicy>();
            builder.Services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
            builder.Services.AddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();
            builder.Services.AddScoped<LookupMusicRequestHandler>();
            builder.Services.AddScoped<MusicTrackEventCommandHandler>();
            builder.Services.AddScoped<ApplyEnrichmentResponseHandler>();

            builder.UseWolverine(opts =>
            {
                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.IncludeType<LocalPipelineHandlers>();
            });

            var host = builder.Build();
            await host.StartAsync();

            return new AsyncLookupHappyPathTestEnvironment(
                host,
                candidateSearch,
                projectionStore);
        }

        public async Task<SearchMusicResponse> SearchAsync(string query)
        {
            using var scope = host.Services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IHandler<SearchMusicRequest, SearchMusicResponse>>();
            return await handler.Handle(
                new SearchMusicRequest(
                    query,
                    Limit.From(5),
                    MinConfidence: null));
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
        }
    }

    public sealed class LocalPipelineHandlers(
        LookupMusicRequestHandler lookupMusicRequestHandler,
        MusicTrackEventCommandHandler musicTrackEventCommandHandler,
        ApplyEnrichmentResponseHandler applyEnrichmentResponseHandler)
    {
        [WolverineHandler]
        public async Task<object[]> Handle(
            LookupMusicRequestDto dto,
            CancellationToken cancellationToken)
        {
            var result = await lookupMusicRequestHandler.ScheduleAsync(
                new Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.LookupMusicRequest(
                    Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.NormalizedSearchQuery.FromText(dto.Query),
                    dto.TrustLevel,
                    dto.RiskScore,
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)),
                cancellationToken);

            return result.ShouldSchedule
                ? [result.Command!.ToDto()]
                : [];
        }

        [WolverineHandler]
        public object Handle(ResolveCanonicalMetadataCommandDto dto)
        {
            return new EnrichmentResponseDto(
                dto.CommandId,
                dto.MusicCatalogId,
                ProviderName.MusicBrainz.Value,
                dto.Priority,
                dto.CreatedAt,
                new SongMetadataDto(
                    "Rare Unknown Song",
                    "Test Artist",
                    "isrc-rare-1",
                    "mbid-rare-1",
                    123000),
                Array.Empty<ExternalReferenceDto>(),
                dto.CorrelationId);
        }

        [WolverineHandler]
        public object Handle(AppleMusicResolutionRequiredMessageDto dto)
        {
            var command = musicTrackEventCommandHandler.Handle(
                new AppleMusicResolutionRequired(
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    CorrelationId.From(dto.CorrelationId),
                    ProviderName.From(dto.SourceProvider),
                    dto.ObservedAt));

            return new ResolveApplePlaybackReferenceCommandDto(
                command.CommandId.Value,
                command.MusicCatalogId.Value,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId.Value);
        }

        [WolverineHandler]
        public object Handle(YouTubeMusicResolutionRequiredMessageDto dto)
        {
            var command = musicTrackEventCommandHandler.Handle(
                new YouTubeMusicResolutionRequired(
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    CorrelationId.From(dto.CorrelationId),
                    ProviderName.From(dto.SourceProvider),
                    dto.ObservedAt));

            return new ResolveYouTubeMusicPlaybackReferenceCommandDto(
                command.CommandId.Value,
                command.MusicCatalogId.Value,
                command.Priority,
                command.CreatedAt,
                command.CorrelationId.Value);
        }

        [WolverineHandler]
        public object Handle(ResolveApplePlaybackReferenceCommandDto dto)
        {
            return new EnrichmentResponseDto(
                dto.CommandId,
                dto.MusicCatalogId,
                ProviderName.AppleMusic.Value,
                dto.Priority,
                dto.CreatedAt,
                null,
                [
                    new ExternalReferenceDto(
                        ProviderName.AppleMusic.Value,
                        new Uri("https://music.apple.com/track/apple-track-1"),
                        "apple-track-1",
                        ReferenceConfidence.Verified.ToString())
                ],
                dto.CorrelationId);
        }

        [WolverineHandler]
        public object Handle(ResolveYouTubeMusicPlaybackReferenceCommandDto dto)
        {
            return new EnrichmentResponseDto(
                dto.CommandId,
                dto.MusicCatalogId,
                ProviderName.YoutubeMusic.Value,
                dto.Priority,
                dto.CreatedAt,
                null,
                [
                    new ExternalReferenceDto(
                        ProviderName.YoutubeMusic.Value,
                        new Uri("https://music.youtube.com/watch?v=yt-track-1"),
                        "yt-track-1",
                        ReferenceConfidence.Verified.ToString())
                ],
                dto.CorrelationId);
        }

        [WolverineHandler]
        public async Task<object[]> Handle(
            EnrichmentResponseDto dto,
            CancellationToken cancellationToken)
        {
            var result = await applyEnrichmentResponseHandler.Handle(
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
                        reference.ExternalId,
                        Enum.Parse<ReferenceConfidence>(reference.Confidence, ignoreCase: true))).ToArray(),
                    CorrelationId.From(dto.CorrelationId)),
                cancellationToken);

            return result.Facts
                .Select(MapFollowUpMessage)
                .Where(message => message is not null)
                .Cast<object>()
                .ToArray();
        }

        private static object? MapFollowUpMessage(MusicTrackFact fact) =>
            fact switch
            {
                AppleMusicResolutionRequired apple => new AppleMusicResolutionRequiredMessageDto(
                    apple.MusicCatalogId.Value,
                    apple.Priority,
                    apple.CorrelationId.Value,
                    apple.SourceProvider.Value,
                    apple.ObservedAt),
                YouTubeMusicResolutionRequired youtube => new YouTubeMusicResolutionRequiredMessageDto(
                    youtube.MusicCatalogId.Value,
                    youtube.Priority,
                    youtube.CorrelationId.Value,
                    youtube.SourceProvider.Value,
                    youtube.ObservedAt),
                _ => null
            };
    }

    private sealed class ProjectionBackedTrackSearchPort(MusicTrackProjectionStoreFake projectionStore) : ITrackSearchPort
    {
        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            Soundtrail.Services.Api.Features.Search.TrackSearch.NormalizedSearchQuery query,
            Limit limit,
            CancellationToken cancellationToken)
        {
            var matches = projectionStore.Projections.Values
                .Where(track => track.CanonicalMetadata is not null)
                .Where(track => Soundtrail.Services.Api.Features.Search.TrackSearch.NormalizedSearchQuery.FromText(
                        $"{track.CanonicalMetadata!.Title} {track.CanonicalMetadata.Artist}")
                    .Value.Contains(query.Value, StringComparison.Ordinal))
                .Take(limit.Value)
                .Select(track => new SearchResult(
                    TrackTitle.From(track.CanonicalMetadata!.Title),
                    ArtistName.From(track.CanonicalMetadata.Artist),
                    string.IsNullOrWhiteSpace(track.CanonicalMetadata.Isrc) ? null : Isrc.From(track.CanonicalMetadata.Isrc),
                    string.IsNullOrWhiteSpace(track.CanonicalMetadata.Mbid) ? null : Mbid.From(track.CanonicalMetadata.Mbid),
                    string.IsNullOrWhiteSpace(track.Apple?.ExternalId) ? null : AppleId.From(track.Apple.ExternalId!),
                    null,
                    ConfidenceScore.From(0.95)))
                .ToArray();

            return Task.FromResult<IReadOnlyList<SearchResult>>(matches);
        }

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}
