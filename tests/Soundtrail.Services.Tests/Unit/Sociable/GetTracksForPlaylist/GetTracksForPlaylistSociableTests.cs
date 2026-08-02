using FluentAssertions;
using Microsoft.Extensions.Options;
using Soundtrail.Adapters.Timing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Operations;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Shared.Contract;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkRequested;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

public sealed class GetTracksForPlaylistSociableTests
{
    [Fact]
    public async Task Given_No_Local_Playlist_Data_When_Requesting_Then_A_Pending_Response_Is_Returned()
    {
        var environment = new GetTracksForPlaylistSociableTestEnvironment();

        var response = await environment.GetTracksForPlaylistAsync();

        response.Should().NotBeNull();
        response!.Tracks.Should().BeEmpty();
        response.Discovery.Should().NotBeNull();
        response.Discovery!.Status.Should().Be("scheduled");
        response.Discovery.NextEligibleAt.Should().Be(environment.Clock.UtcNow.AddSeconds(15));
        environment.CommandBus.Messages.Should().ContainSingle(message => message is RequestKnownMusicDataMessage);
    }

    [Fact]
    public async Task Given_No_Local_Playlist_Data_When_Processing_Discovery_Then_Metadata_And_Playability_Are_Returned()
    {
        var environment = new GetTracksForPlaylistSociableTestEnvironment();
        await environment.GetTracksForPlaylistAsync();

        await environment.PumpAsync();

        var response = await environment.GetTracksForPlaylistAsync();

        response.Should().NotBeNull();
        response!.Tracks.Should().HaveCount(4);
        response.Tracks.Select(track => track.Title).Should().BeEquivalentTo([
            "Midnight Signals",
            "Static Hearts",
            "Glass Cities (Radio Edit)",
            "Golden Echo - Radio Edit"
        ]);
        response.Tracks.Where(track => track.Playable)
            .Select(track => track.Title)
            .Should()
            .BeEquivalentTo(["Midnight Signals", "Glass Cities (Radio Edit)"]);
        response.Tracks.SelectMany(track => track.StreamingLocations)
            .Select(location => location.Url)
            .Should()
            .BeEquivalentTo([
                "https://open.spotify.com/track/midnight-signals",
                "https://music.youtube.com/watch?v=glass-cities-radio"
            ]);
        response.Discovery.Should().NotBeNull();
        response.Discovery!.Status.Should().Be("completed");
    }
}

internal sealed class GetTracksForPlaylistSociableTestEnvironment
{
    private readonly SociableCatalogState state;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;
    private readonly GetTracksForPlaylistHandler getTracksForPlaylistHandler;
    private readonly OnKnownMusicDataRequestedHandler knownMusicDataRequestedHandler;
    private readonly OnUnknownMusicDataRequestedHandler unknownMusicDataRequestedHandler;
    private readonly OnMusicAssessmentRequiredHandler musicAssessmentRequiredHandler;
    private readonly LookupWorkReadyHandler lookupWorkReadyHandler;
    private readonly LookupCompletedHandler lookupCompletedHandler;
    private readonly LookupPlaylistTracksByProviderHandler lookupPlaylistTracksByProviderHandler;
    private readonly LookupMusicbrainzSearchResultsHandler lookupMusicbrainzSearchResultsHandler;
    private readonly LookupStreamingLocationByIsrcHandler lookupStreamingLocationByIsrcHandler;
    private readonly LookupStreamingLocationByTrackMetadataHandler lookupStreamingLocationByTrackMetadataHandler;
    private readonly WorkRequestedProjectorHandler workRequestedProjectorHandler;
    private readonly WorkScheduledProjectorHandler workScheduledProjectorHandler;
    private readonly WorkFeedbackChangedProjectorHandler workFeedbackChangedProjectorHandler;
    private readonly CatalogItemChangedProjectorHandler catalogItemChangedProjectorHandler;
    private readonly ArtistCatalogChangedProjectorHandler artistCatalogChangedProjectorHandler;
    private readonly CatalogSearchCandidateChangedProjectorHandler catalogSearchCandidateChangedProjectorHandler;
    private readonly CatalogTrackChangedProjectorHandler catalogTrackChangedProjectorHandler;
    private readonly PlaylistTracksDiscoveredProjectorHandler playlistTracksDiscoveredProjectorHandler;

    public GetTracksForPlaylistSociableTestEnvironment()
    {
        Clock = new ClockFake();
        CommandBus = new CommandBusFake();
        state = new SociableCatalogState(Clock);
        discoveryRepository = new InMemoryEventStreamRepository<CatalogWorkId>(ProjectDiscoveryEventsAsync);
        artistRepository = new InMemoryEventStreamRepository<ArtistId>();

        var planner = new WorkPlanner();
        var assessmentPolicy = new PlanningAssessmentPolicy(Options.Create(new PlanningAssessmentOptions()));

        getTracksForPlaylistHandler = new GetTracksForPlaylistHandler(state, CommandBus, state, Clock);
        knownMusicDataRequestedHandler = new OnKnownMusicDataRequestedHandler(planner, discoveryRepository);
        unknownMusicDataRequestedHandler = new OnUnknownMusicDataRequestedHandler(planner, state, discoveryRepository);
        musicAssessmentRequiredHandler = new OnMusicAssessmentRequiredHandler(assessmentPolicy, state, discoveryRepository);
        lookupWorkReadyHandler = new LookupWorkReadyHandler(CommandBus);
        lookupCompletedHandler = new LookupCompletedHandler(discoveryRepository, CommandBus);
        lookupPlaylistTracksByProviderHandler = new LookupPlaylistTracksByProviderHandler(state, Clock, CommandBus);
        lookupMusicbrainzSearchResultsHandler = new LookupMusicbrainzSearchResultsHandler(state, Clock, CommandBus);
        lookupStreamingLocationByIsrcHandler = new LookupStreamingLocationByIsrcHandler(state, state, Clock, CommandBus);
        lookupStreamingLocationByTrackMetadataHandler = new LookupStreamingLocationByTrackMetadataHandler(state, state, Clock, CommandBus);

        workRequestedProjectorHandler = new WorkRequestedProjectorHandler(CommandBus);
        workScheduledProjectorHandler = new WorkScheduledProjectorHandler(CommandBus);
        workFeedbackChangedProjectorHandler = new WorkFeedbackChangedProjectorHandler(state);
        catalogItemChangedProjectorHandler = new CatalogItemChangedProjectorHandler(artistRepository);
        artistCatalogChangedProjectorHandler = new ArtistCatalogChangedProjectorHandler(artistRepository, state);
        catalogSearchCandidateChangedProjectorHandler = new CatalogSearchCandidateChangedProjectorHandler(state);
        catalogTrackChangedProjectorHandler = new CatalogTrackChangedProjectorHandler(state);
        playlistTracksDiscoveredProjectorHandler = new PlaylistTracksDiscoveredProjectorHandler(state);
    }

    public PlaylistId PlaylistId { get; } = PlaylistId.FromPlaylistName("world_top_100");

    public ClockFake Clock { get; }

    public CommandBusFake CommandBus { get; }

    public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync() =>
        getTracksForPlaylistHandler.Handle(new GetTracksForPlaylistRequest(PlaylistId), CancellationToken.None);

    public async Task PumpAsync()
    {
        for (var iteration = 0; iteration < 500 && CommandBus.TryDequeue(out var message); iteration++)
        {
            await HandleAsync(message, CancellationToken.None);
        }

        CommandBus.Messages.Should().BeEmpty("the sociable message pump should drain all known work");
    }

    private async Task HandleAsync(IMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case RequestKnownMusicDataMessage known:
                await knownMusicDataRequestedHandler.Handle(IncomingMessage<RequestKnownMusicDataMessage>.Create(known), cancellationToken);
                break;
            case RequestUnknownMusicDataMessage unknown:
                await unknownMusicDataRequestedHandler.Handle(IncomingMessage<RequestUnknownMusicDataMessage>.Create(unknown), cancellationToken);
                break;
            case AssessWorkMessage assess:
                await musicAssessmentRequiredHandler.Handle(IncomingMessage<AssessWorkMessage>.Create(assess), cancellationToken);
                break;
            case DispatchLookupWork dispatch:
                await lookupWorkReadyHandler.Handle(IncomingMessage<DispatchLookupWork>.Create(dispatch), cancellationToken);
                break;
            case LookupPlaylistTracksByProviderMessage lookupPlaylist:
                await lookupPlaylistTracksByProviderHandler.Handle(IncomingMessage<LookupPlaylistTracksByProviderMessage>.Create(lookupPlaylist), cancellationToken);
                break;
            case LookupMusicbrainzSearchResultsMessage lookupSearch:
                await lookupMusicbrainzSearchResultsHandler.Handle(IncomingMessage<LookupMusicbrainzSearchResultsMessage>.Create(lookupSearch), cancellationToken);
                break;
            case LookupStreamingLocationByIsrcMessage lookupByIsrc:
                await lookupStreamingLocationByIsrcHandler.Handle(IncomingMessage<LookupStreamingLocationByIsrcMessage>.Create(lookupByIsrc), cancellationToken);
                break;
            case LookupStreamingLocationByTrackMetadataMessage lookupByMetadata:
                await lookupStreamingLocationByTrackMetadataHandler.Handle(IncomingMessage<LookupStreamingLocationByTrackMetadataMessage>.Create(lookupByMetadata), cancellationToken);
                break;
            case CatalogLookupCompleted completed:
                await lookupCompletedHandler.Handle(IncomingMessage<CatalogLookupCompleted>.Create(completed), cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unhandled sociable message '{message.GetType().Name}'.");
        }
    }

    private async Task ProjectDiscoveryEventsAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            switch (@event)
            {
                case WorkRequested workRequested:
                    await workFeedbackChangedProjectorHandler.Handle(workRequested, cancellationToken);
                    await workRequestedProjectorHandler.Handle(workRequested, cancellationToken);
                    break;
                case WorkPriorityRaised workPriorityRaised:
                    await workRequestedProjectorHandler.Handle(workPriorityRaised, cancellationToken);
                    break;
                case WorkScheduled workScheduled:
                    await workFeedbackChangedProjectorHandler.Handle(workScheduled, cancellationToken);
                    await workScheduledProjectorHandler.Handle(workScheduled, cancellationToken);
                    break;
                case WorkDeferred workDeferred:
                    await workFeedbackChangedProjectorHandler.Handle(workDeferred, cancellationToken);
                    break;
                case WorkCompleted workCompleted:
                    await workFeedbackChangedProjectorHandler.Handle(workCompleted, cancellationToken);
                    break;
                case WorkRejected workRejected:
                    await workFeedbackChangedProjectorHandler.Handle(workRejected, cancellationToken);
                    break;
                case WorkIgnored workIgnored:
                    await workFeedbackChangedProjectorHandler.Handle(workIgnored, cancellationToken);
                    break;
                case WorkAttemptFailed workAttemptFailed:
                    await workFeedbackChangedProjectorHandler.Handle(workAttemptFailed, cancellationToken);
                    break;
                case TrackDiscovered trackDiscovered:
                    await catalogItemChangedProjectorHandler.Handle(trackDiscovered, cancellationToken);
                    await catalogSearchCandidateChangedProjectorHandler.Handle(trackDiscovered, cancellationToken);
                    await artistCatalogChangedProjectorHandler.Handle(trackDiscovered.Hierarchy.ArtistId!.Value, cancellationToken);
                    await catalogTrackChangedProjectorHandler.Handle(trackDiscovered.Track.TrackId, cancellationToken);
                    break;
                case ArtistDiscovered artistDiscovered:
                    await catalogItemChangedProjectorHandler.Handle(artistDiscovered, cancellationToken);
                    await catalogSearchCandidateChangedProjectorHandler.Handle(artistDiscovered, cancellationToken);
                    await artistCatalogChangedProjectorHandler.Handle(artistDiscovered.Artist.Id, cancellationToken);
                    break;
                case AlbumDiscovered albumDiscovered:
                    await catalogItemChangedProjectorHandler.Handle(albumDiscovered, cancellationToken);
                    await catalogSearchCandidateChangedProjectorHandler.Handle(albumDiscovered, cancellationToken);
                    await artistCatalogChangedProjectorHandler.Handle(ArtistId.From(albumDiscovered.Album.AlbumId.ArtistId), cancellationToken);
                    break;
                case StreamingLocationDiscovered streamingLocationDiscovered:
                    await catalogItemChangedProjectorHandler.Handle(streamingLocationDiscovered, cancellationToken);
                    await artistCatalogChangedProjectorHandler.Handle(streamingLocationDiscovered.Hierarchy.ArtistId!.Value, cancellationToken);
                    await catalogTrackChangedProjectorHandler.Handle(streamingLocationDiscovered.MusicCatalogId.AsTrack(), cancellationToken);
                    break;
                case PlaylistTracksDiscovered playlistTracksDiscovered:
                    await playlistTracksDiscoveredProjectorHandler.Handle(playlistTracksDiscovered, cancellationToken);
                    break;
            }
        }
    }
}

internal sealed class SociableCatalogState(
    ClockFake clock) :
    IGetTracksForPlaylistPort,
    IDiscoveryFeedbackPort,
    IStoreDiscoveryFeedbackPort,
    IStorePlaylistTracksReadModelPort,
    IStoreArtistCatalogReadModelPort,
    IStoreCatalogSearchCandidatePort,
    IReadPlaylistTracksByProviderPort,
    IReadCatalogEntriesBySearchCriteriaPort,
    IReadTrackForLookupPort,
    IReadStreamingLocationByProviderPort,
    IDiscoveryPlanningProjectionReader,
    ISearchForCandidates
{
    private readonly Dictionary<string, CatalogPlaylistTracksRecordDto> playlists = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CatalogTrackRecordDto> tracks = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ArtistId> trackArtists = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CatalogDiscoveryFeedbackRecordDto> feedback = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CatalogSearchCandidateProjection> candidates = new(StringComparer.Ordinal);

    public Task<GetTracksForPlaylistResponse?> GetTracksForPlaylistAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken)
    {
        if (!playlists.TryGetValue(playlistId.Value, out var record))
        {
            return Task.FromResult<GetTracksForPlaylistResponse?>(null);
        }

        return Task.FromResult<GetTracksForPlaylistResponse?>(new GetTracksForPlaylistResponse(
            playlistId,
            record.Tracks
                .Select(static track => new GetTracksForPlaylistTrackResponse(
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
                        .Select(static location => new StreamingLocationResponse(
                            location.Provider,
                            location.ExternalId,
                            location.Url))
                        .ToArray()))
                .ToArray()));
    }

    public Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken)
    {
        var targetId = target.NormalisedIdentifier;
        if (!feedback.TryGetValue(targetId, out var record))
        {
            return Task.FromResult<DiscoveryFeedbackResponse?>(null);
        }

        var priority = Enum.Parse<LookupPriorityBand>(record.Priority);
        return Task.FromResult<DiscoveryFeedbackResponse?>(new DiscoveryFeedbackResponse(
            record.Status,
            priority,
            record.NextEligibleAtUtc,
            record.EarliestExpectedCompletionAtUtc,
            record.Reason,
            record.UpdatedAtUtc));
    }

    public Task StoreAsync(WorkRequested @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "scheduled", @event.Priority, @event.RequestedAt.AddSeconds(15), @event.RequestedAt.AddSeconds(75), "Work has been requested.", @event.RequestedAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkScheduled @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "scheduled", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.ScheduledAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkDeferred @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "deferred", @event.Priority, @event.NextEligibleAt, null, @event.Reason, @event.DeferredAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkCompleted @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "completed", @event.Priority, null, null, @event.Reason, @event.CompletedAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkRejected @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "rejected", @event.Priority, null, null, @event.Reason, @event.RejectedAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkIgnored @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "ignored", @event.Priority, @event.NextEligibleAt, @event.EarliestExpectedCompletionAt, @event.Reason, @event.IgnoredAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(WorkAttemptFailed @event, CancellationToken cancellationToken)
    {
        StoreFeedback(@event.Target, "failed", LookupPriorityBand.Low, clock.UtcNow.AddSeconds(30), null, @event.Reason, @event.FailedAt);
        return Task.CompletedTask;
    }

    public Task StoreAsync(PlaylistTracksDiscovered @event, CancellationToken cancellationToken)
    {
        var existingTrackIds = playlists.TryGetValue(@event.PlaylistId.Value, out var existing)
            ? existing.TrackIds
            : [];
        var trackIds = MergeTrackIds(existingTrackIds, @event.Tracks.Select(static trackId => trackId.Value));
        playlists[@event.PlaylistId.Value] = BuildPlaylistRecord(@event.PlaylistId.Value, trackIds, @event.ObservedAt);
        return Task.CompletedTask;
    }

    public Task RepairTrackAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        foreach (var playlist in playlists.Values.Where(playlist => ContainsSameBaseTrack(playlist, trackId)).ToArray())
        {
            playlists[playlist.PlaylistId] = BuildPlaylistRecord(playlist.PlaylistId, playlist.TrackIds, playlist.UpdatedAt);
        }

        return Task.CompletedTask;
    }

    public Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
    {
        foreach (var track in readModel.Tracks)
        {
            tracks[track.TrackId.Value] = new CatalogTrackRecordDto
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
                StreamingLocations = track.StreamingLocations
                    .Select(static location => new CatalogStreamingLocationRecordDto
                    {
                        Provider = location.Provider.StableValue,
                        ExternalId = location.ExternalId,
                        Url = location.Url
                    })
                    .ToArray(),
                UpdatedAt = readModel.UpdatedAt
            };
            trackArtists[track.TrackId.Value] = readModel.ArtistId;
        }

        return Task.CompletedTask;
    }

    public Task StoreAsync(CatalogSearchCandidateProjection projection, CancellationToken cancellationToken)
    {
        candidates[projection.CatalogItemId] = projection;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TrackReference>> ReadAsync(
        PlaylistId playlistId,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        if (playlistId.Value != PlaylistId.FromPlaylistName("world_top_100").Value || provider != ProviderName.Spotify)
        {
            return Task.FromResult<IReadOnlyList<TrackReference>>([]);
        }

        return Task.FromResult<IReadOnlyList<TrackReference>>([
            new(ArtistName.From("Aurora Lane"), "Midnight Signals"),
            new(ArtistName.From("Paper Tigers"), "Static Hearts"),
            new(ArtistName.From("Neon Harbour"), "Glass Cities"),
            new(ArtistName.From("Saturn Kids"), "Golden Echo")
        ]);
    }

    public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(
        SearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<CatalogDiscoveryEntry>>(
            searchCriteria.Query switch
            {
                "Midnight Signals Aurora Lane" => [Track("Aurora Lane", "Midnight Signals", "Midnight Signals", new DateOnly(2023, 11, 10), null, 214000)],
                "Static Hearts Paper Tigers" => [Track("Paper Tigers", "Static Hearts", "Static Hearts", new DateOnly(2022, 9, 16), null, 198000)],
                "Glass Cities Neon Harbour" => [Track("Neon Harbour", "Glass Cities (Radio Edit)", "Glass Cities Remixes", new DateOnly(2024, 6, 23), "Radio Edit", 231000)],
                "Golden Echo Saturn Kids" => [Track("Saturn Kids", "Golden Echo - Radio Edit", "Golden Echo Radio Release", new DateOnly(2024, 2, 14), "Radio Edit", 244000)],
                _ => []
            });
    }

    public Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        if (!tracks.TryGetValue(trackId.Value, out var track) || !trackArtists.TryGetValue(trackId.Value, out var artistId))
        {
            return Task.FromResult<TrackLookupContext?>(null);
        }

        return Task.FromResult<TrackLookupContext?>(new TrackLookupContext(
            artistId,
            trackId,
            track.Title,
            track.ArtistName,
            track.Isrc));
    }

    public Task<Uri?> ReadByIsrcAsync(string isrc, ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult<Uri?>(null);

    public Task<Uri?> ReadByTrackMetadataAsync(
        string artistName,
        string trackTitle,
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        var url = (artistName, trackTitle, provider.StableValue) switch
        {
            ("Aurora Lane", "Midnight Signals", "spotify") => "https://open.spotify.com/track/midnight-signals",
            ("Neon Harbour", "Glass Cities (Radio Edit)", "youtubeMusic") => "https://music.youtube.com/watch?v=glass-cities-radio",
            _ => null
        };

        return Task.FromResult(url is null ? null : new Uri(url));
    }

    public Task<DiscoveryPlanningProjection> ReadAsync(
        EnrichmentTarget target,
        CancellationToken cancellationToken) =>
        Task.FromResult(new DiscoveryPlanningProjection(false, null, 0, 0));

    public CandidatesResult Search(EnrichmentTarget target)
    {
        if (target is not EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria))
        {
            return new CandidatesResult.None();
        }

        var normalized = searchCriteria.NormalisedIdentifier["search:".Length..];
        var matches = candidates.Values
            .Where(candidate => candidate.CandidateKind == "track")
            .Where(candidate => StringNormalizationExtensions.Normalize(candidate.SearchText) == normalized)
            .Select(candidate => new ScoredCandidate(new CatalogItemId.Track(TrackId.From(candidate.CatalogItemId)), 100))
            .ToList();

        return matches.Count == 0
            ? new CandidatesResult.None()
            : new CandidatesResult.Results(CandidateList.From(matches));
    }

    private void StoreFeedback(
        EnrichmentTarget target,
        string status,
        LookupPriorityBand priority,
        DateTimeOffset? nextEligibleAt,
        DateTimeOffset? earliestExpectedCompletionAt,
        string reason,
        DateTimeOffset updatedAt)
    {
        feedback[target.NormalisedIdentifier] = new CatalogDiscoveryFeedbackRecordDto
        {
            Id = CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier),
            TargetId = target.NormalisedIdentifier,
            Status = status,
            Priority = priority.ToString(),
            NextEligibleAtUtc = nextEligibleAt,
            EarliestExpectedCompletionAtUtc = earliestExpectedCompletionAt,
            Reason = reason,
            UpdatedAtUtc = updatedAt
        };
    }

    private CatalogPlaylistTracksRecordDto BuildPlaylistRecord(
        string playlistId,
        IReadOnlyList<string> trackIds,
        DateTimeOffset updatedAt)
    {
        var playlistTrackIds = trackIds.Select(TrackId.From).ToArray();
        var requestedBases = playlistTrackIds
            .Select(TrackIdIndexProjection.From)
            .DistinctBy(static projection => (projection.BaseHigh, projection.BaseLow))
            .ToArray();
        var tracksByBase = tracks.Values
            .Select(track => (Track: track, Projection: TrackIdIndexProjection.From(TrackId.From(track.TrackId))))
            .Where(entry => requestedBases.Any(requested => requested.SharesBaseWith(entry.Projection)))
            .GroupBy(entry => (entry.Projection.BaseHigh, entry.Projection.BaseLow))
            .ToDictionary(static group => group.Key, static group => group.Select(static entry => entry.Track).ToArray());

        return new CatalogPlaylistTracksRecordDto
        {
            Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
            PlaylistId = playlistId,
            TrackIds = trackIds.ToArray(),
            Tracks = playlistTrackIds
                .Select(trackId => SelectPreferredTrack(tracksByBase, trackId))
                .Where(static track => track is not null)
                .Select(static track => new CatalogPlaylistTrackRecordDto
                {
                    TrackId = track!.TrackId,
                    MusicCatalogId = track.MusicCatalogId,
                    Title = track.Title,
                    ArtistName = track.ArtistName,
                    AlbumTitle = track.AlbumTitle,
                    DurationMs = track.DurationMs,
                    Isrc = track.Isrc,
                    ReleaseDate = track.ReleaseDate,
                    ReleaseType = track.ReleaseType,
                    ArtworkUrl = track.ArtworkUrl,
                    StreamingLocations = track.StreamingLocations
                })
                .ToArray(),
            UpdatedAt = updatedAt
        };
    }

    private static string[] MergeTrackIds(
        IReadOnlyCollection<string>? existingTrackIds,
        IEnumerable<string> discoveredTrackIds)
    {
        if (existingTrackIds is null || existingTrackIds.Count == 0)
        {
            return discoveredTrackIds.Distinct(StringComparer.Ordinal).ToArray();
        }

        var mergedTrackIds = new List<string>(existingTrackIds.Count);
        var seenTrackIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var trackId in existingTrackIds)
        {
            if (seenTrackIds.Add(trackId))
            {
                mergedTrackIds.Add(trackId);
            }
        }

        foreach (var trackId in discoveredTrackIds)
        {
            if (seenTrackIds.Add(trackId))
            {
                mergedTrackIds.Add(trackId);
            }
        }

        return mergedTrackIds.ToArray();
    }

    private static CatalogPlaylistTrackRecordDto? SelectPreferredTrack(
        IReadOnlyDictionary<(ulong BaseHigh, ulong BaseLow), CatalogTrackRecordDto[]> tracksByBase,
        TrackId requestedTrackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(requestedTrackId);
        if (!tracksByBase.TryGetValue((requestedProjection.BaseHigh, requestedProjection.BaseLow), out var candidates))
        {
            return null;
        }

        var track = candidates.FirstOrDefault(candidate => string.Equals(candidate.TrackId, requestedTrackId.Value, StringComparison.Ordinal))
            ?? candidates
                .Select(candidate => (Track: candidate, Projection: TrackIdIndexProjection.From(TrackId.From(candidate.TrackId))))
                .OrderBy(entry => entry.Projection.GetDistanceTo(requestedProjection))
                .ThenByDescending(static entry => entry.Track.UpdatedAt)
                .Select(static entry => entry.Track)
                .FirstOrDefault();

        return track is null
            ? null
            : new CatalogPlaylistTrackRecordDto
            {
                TrackId = track.TrackId,
                MusicCatalogId = track.MusicCatalogId,
                Title = track.Title,
                ArtistName = track.ArtistName,
                AlbumTitle = track.AlbumTitle,
                DurationMs = track.DurationMs,
                Isrc = track.Isrc,
                ReleaseDate = track.ReleaseDate,
                ReleaseType = track.ReleaseType,
                ArtworkUrl = track.ArtworkUrl,
                StreamingLocations = track.StreamingLocations
            };
    }

    private static bool ContainsSameBaseTrack(CatalogPlaylistTracksRecordDto record, TrackId trackId)
    {
        var requestedProjection = TrackIdIndexProjection.From(trackId);
        return record.TrackIds
            .Select(TrackId.From)
            .Select(TrackIdIndexProjection.From)
            .Any(existingProjection => existingProjection.SharesBaseWith(requestedProjection));
    }

    private static CatalogDiscoveryEntry Track(
        string artistName,
        string title,
        string albumTitle,
        DateOnly releaseDate,
        string? releaseType,
        int durationMs)
    {
        var trackId = TrackId.TryCreate(artistName, title, albumTitle, releaseDate, releaseType) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unsupported track id creation result.")
        };
        var track = new Track(trackId)
        {
            Title = title,
            ArtistName = artistName,
            AlbumTitle = albumTitle,
            DurationMs = durationMs,
            ReleaseDate = releaseDate,
            ReleaseType = releaseType,
            UpdatedAt = new DateTimeOffset(2026, 7, 24, 12, 1, 0, TimeSpan.Zero)
        };

        return new CatalogDiscoveryEntry(ArtistId.From($"musicbrainz-artist:{StringNormalizationExtensions.Normalize(artistName)}"), new CatalogItem.MusicTrack(track));
    }
}

internal sealed class InMemoryEventStreamRepository<TStreamId>(
    Func<IReadOnlyList<IDomainEvent>, CancellationToken, Task>? onAppend = null) : IEventStreamRepository<TStreamId>
    where TStreamId : IValueType
{
    private readonly Dictionary<string, List<IDomainEvent>> eventsByStream = new(StringComparer.Ordinal);
    private readonly HashSet<string> operationIds = new(StringComparer.Ordinal);

    public Task<LoadedEventStream<TStreamId>> LoadAsync(TStreamId streamId, CancellationToken cancellationToken)
    {
        var streamKey = streamId.StableValue;
        var events = eventsByStream.TryGetValue(streamKey, out var existing)
            ? existing.ToArray()
            : [];
        return Task.FromResult(new LoadedEventStream<TStreamId>(streamId, events.Length, events));
    }

    public async Task<AppendResult> AppendAsync(
        LoadedEventStream<TStreamId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        if (operationId is not null && !operationIds.Add(operationId.Value.StableValue))
        {
            return new AppendResult(false, stream.Version, [], AppendOutcome.DuplicateOperation);
        }

        var streamKey = stream.StreamId.StableValue;
        var existing = eventsByStream.GetValueOrDefault(streamKey);
        if (existing is null)
        {
            existing = [];
            eventsByStream[streamKey] = existing;
        }

        if (existing.Count != stream.Version)
        {
            return new AppendResult(false, existing.Count, [], AppendOutcome.VersionMismatch);
        }

        existing.AddRange(events);
        if (onAppend is not null && events.Count > 0)
        {
            await onAppend(events, cancellationToken);
        }

        return new AppendResult(true, existing.Count, events, AppendOutcome.Appended);
    }
}

internal sealed class CommandBusFake : ICommandBus
{
    private readonly Queue<IMessage> queue = [];

    public IReadOnlyCollection<IMessage> Messages => queue.ToArray();

    public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        queue.Enqueue(message);
        return Task.CompletedTask;
    }

    public bool TryDequeue(out IMessage message) => queue.TryDequeue(out message!);
}

internal sealed class ClockFake : IClockPort
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
}
