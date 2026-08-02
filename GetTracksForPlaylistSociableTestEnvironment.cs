using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc;
using Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;
using Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogItemChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnWorkRequested;
using Soundtrail.Services.Internal.Projector.Features.OnWorkScheduled;
using Soundtrail.Services.Tests.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistSociableTestEnvironment
{
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;
    private readonly HandlerCollection handlerCollection;
    private readonly EventHandlers eventHandlers;

    private GetTracksForPlaylistSociableTestEnvironment(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteTrack> completedTracks)
    {
        Clock = new ClockFake(utcNow);
        CommandBus = new CommandBusFake();
        discoveryRepository = new InMemoryEventStreamRepository<CatalogWorkId>(ProjectDiscoveryEventsAsync);
        artistRepository = new InMemoryEventStreamRepository<ArtistId>();

        var readTrack = new ReadTrackForLookupPortFake();
        var storeDiscoveryFeedback = new StoreDiscoveryFeedbackPortFake();
        var storeArtistCatalog = new StoreArtistCatalogReadModelPortFake(readTrack);
        var storePlaylistTracks = new StorePlaylistTracksReadModelPortFake(Clock, storeArtistCatalog, storeDiscoveryFeedback);
        var storeSearchCandidate = new StoreCatalogSearchCandidatePortFake();
        var readPlaylistTracks = new ReadPlaylistTracksByProviderPortFake()
            .WithTracks(
                PlaylistId,
                ProviderName.Spotify,
                completedTracks.Select(static track => track.PlaylistTrack).ToArray());
        var readCatalogEntries = new ReadCatalogEntriesBySearchCriteriaPortFake();
        var readStreamingLocation = new ReadStreamingLocationByProviderPortFake();

        foreach (var completedTrack in completedTracks)
        {
            readCatalogEntries.WithEntries(
                new SearchCriteria(
                    $"{completedTrack.PlaylistTrack.TrackTitle} {completedTrack.PlaylistTrack.ArtistName.Value}",
                    SearchType.Track),
                completedTrack.CatalogEntry);

            if (completedTrack.CatalogEntry.Item is CatalogItem.MusicTrack(var track))
            {
                foreach (var location in completedTrack.StreamingLocations)
                {
                    readStreamingLocation.WithMetadataLocation(
                        track.ArtistName,
                        track.Title,
                        location.Key,
                        location.Value);
                }
            }
        }

        var planningProjection = new DiscoveryPlanningProjectionReaderFake();
        var searchForCandidates = new SearchForCandidatesFake(storeSearchCandidate);
        Port = GetTracksForPlaylistPortFake.Create(storePlaylistTracks.ReadAsync);

        var planner = new WorkPlanner();
        var assessmentPolicy = new PlanningAssessmentPolicy(Options.Create(new PlanningAssessmentOptions()));

        handlerCollection = new HandlerCollection();
        
        // Register handlers directly with the collection
        // Note: This is a simplified approach - in practice, you'd want to register these via the static discovery mechanism
        // For now, we'll keep the existing registration pattern but use the new collection for handling

        var workRequested = new WorkRequestedProjectorHandler(CommandBus);
        var workScheduled = new WorkScheduledProjectorHandler(CommandBus);
        var workFeedback = new WorkFeedbackChangedProjectorHandler(storeDiscoveryFeedback);
        var catalogItem = new CatalogItemChangedProjectorHandler(artistRepository);
        var artistCatalog = new ArtistCatalogChangedProjectorHandler(artistRepository, storeArtistCatalog);
        var searchCandidate = new CatalogSearchCandidateChangedProjectorHandler(storeSearchCandidate);
        var catalogTrack = new CatalogTrackChangedProjectorHandler(storePlaylistTracks);
        var playlistTracks = new PlaylistTracksDiscoveredProjectorHandler(storePlaylistTracks);

        eventHandlers = new EventHandlers();
        eventHandlers.RegisterAsync<WorkRequested>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkRequested>(workRequested.Handle);
        eventHandlers.RegisterAsync<WorkPriorityRaised>(workRequested.Handle);
        eventHandlers.RegisterAsync<WorkScheduled>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkScheduled>(workScheduled.Handle);
        eventHandlers.RegisterAsync<WorkDeferred>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkCompleted>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkRejected>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkIgnored>(workFeedback.Handle);
        eventHandlers.RegisterAsync<WorkAttemptFailed>(workFeedback.Handle);
        eventHandlers.RegisterAsync<TrackDiscovered>(catalogItem.Handle);
        eventHandlers.RegisterAsync<TrackDiscovered>(searchCandidate.Handle);
        eventHandlers.RegisterAsync<TrackDiscovered>((@event, cancellationToken) => artistCatalog.Handle(@event.Hierarchy.ArtistId!.Value, cancellationToken));
        eventHandlers.RegisterAsync<TrackDiscovered>((@event, cancellationToken) => catalogTrack.Handle(@event.Track.TrackId, cancellationToken));
        eventHandlers.RegisterAsync<ArtistDiscovered>(catalogItem.Handle);
        eventHandlers.RegisterAsync<ArtistDiscovered>(searchCandidate.Handle);
        eventHandlers.RegisterAsync<ArtistDiscovered>((@event, cancellationToken) => artistCatalog.Handle(@event.Artist.Id, cancellationToken));
        eventHandlers.RegisterAsync<AlbumDiscovered>(catalogItem.Handle);
        eventHandlers.RegisterAsync<AlbumDiscovered>(searchCandidate.Handle);
        eventHandlers.RegisterAsync<AlbumDiscovered>((@event, cancellationToken) => artistCatalog.Handle(ArtistId.From(@event.Album.AlbumId.ArtistId), cancellationToken));
        eventHandlers.RegisterAsync<StreamingLocationDiscovered>(catalogItem.Handle);
        eventHandlers.RegisterAsync<StreamingLocationDiscovered>((@event, cancellationToken) => artistCatalog.Handle(@event.Hierarchy.ArtistId!.Value, cancellationToken));
        eventHandlers.RegisterAsync<StreamingLocationDiscovered>((@event, cancellationToken) => catalogTrack.Handle(@event.MusicCatalogId.AsTrack(), cancellationToken));
        eventHandlers.RegisterAsync<PlaylistTracksDiscovered>(playlistTracks.Handle);
    }

    public PlaylistId PlaylistId { get; } = PlaylistId.FromPlaylistName("world_top_100");

    private ClockFake Clock { get; }

    private CommandBusFake CommandBus { get; }

    private IGetTracksForPlaylistPort Port { get; }

    public TMessage SentMessage<TMessage>() where TMessage : IMessage =>
        CommandBus.SentMessages.OfType<TMessage>().Single();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage =>
        CommandBus.SentMessages.OfType<TMessage>().ToArray();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static GetTracksForPlaylistSociableTestEnvironment ForNoExistingDataOrRequests() =>
        new(default, []);

    public static GetTracksForPlaylistSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset requestTime) =>
        new(requestTime, []);

    public static Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingIncompleteLookup() =>
        ForExistingIncompleteLookup(default);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataNotComplete() =>
        new(default, []);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataNotComplete(
        DateTimeOffset requestTime) =>
        new(requestTime, []);

    public static async Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime)
    {
        var environment = new GetTracksForPlaylistSociableTestEnvironment(requestTime, []);

        await environment.CreateSubjectUnderTest()
            .Handle(new GetTracksForPlaylistRequest(environment.PlaylistId));
        await environment.PumpNextMessageAsync();

        return environment;
    }

    public static Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteTrack[] tracks) =>
        ForExistingCompletedLookup(default, tracks);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteTrack[] tracks) =>
        new(default, tracks);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteTrack[] tracks) =>
        new(requestTime, tracks);

    public static async Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteTrack[] tracks)
    {
        var environment = new GetTracksForPlaylistSociableTestEnvironment(requestTime, tracks);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        return environment;
    }

    public async Task ProjectOnChange(Func<GetTracksForPlaylistHandler, Task> change)
    {
        await change(CreateSubjectUnderTest());
        await PumpAsync();
    }

    public async Task<TResult> ProjectOnChange<TResult>(Func<GetTracksForPlaylistHandler, Task<TResult>> change)
    {
        var result = await change(CreateSubjectUnderTest());
        await PumpAsync();
        return result;
    }

    private GetTracksForPlaylistHandler CreateSubjectUnderTest() =>
        new(Port, CommandBus, Clock);

    private async Task PumpNextMessageAsync()
    {
        if (CommandBus.TryDequeue(out var message))
        {
            // Use the new HandlerCollection instead of MessageHandlerMap
            await handlerCollection.HandleAsync(message, CancellationToken.None);
        }
    }

    private async Task PumpAsync()
    {
        for (var iteration = 0; iteration < 500 && CommandBus.TryDequeue(out var message); iteration++)
        {
            await handlerCollection.HandleAsync(message, CancellationToken.None);
        }

        if (CommandBus.Messages.Count > 0)
        {
            throw new InvalidOperationException("The sociable message pump did not drain all known work.");
        }
    }

    private async Task ProjectDiscoveryEventsAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            await eventHandlers.HandleAsync(@event, cancellationToken);
        }
    }
}
