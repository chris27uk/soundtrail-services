using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.Composition;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist;

internal sealed class GetTracksForPlaylistSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetTracksForPlaylistHandler sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private GetTracksForPlaylistSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetTracksForPlaylistHandler sut,
        PlaylistId playlistId,
        StoreDiscoveryFeedbackPortFake discoveryFeedback,
        StorePlaylistTracksReadModelPortFake playlistTracks,
        StoreCatalogSearchCandidatePortFake searchCandidate,
        InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository,
        InMemoryEventStreamRepository<ArtistId> artistRepository)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        PlaylistId = playlistId;
        DiscoveryFeedback = discoveryFeedback;
        PlaylistTracks = playlistTracks;
        SearchCandidate = searchCandidate;
        this.discoveryRepository = discoveryRepository;
        this.artistRepository = artistRepository;
    }

    public PlaylistId PlaylistId { get; }

    public StoreDiscoveryFeedbackPortFake DiscoveryFeedback { get; }

    public StorePlaylistTracksReadModelPortFake PlaylistTracks { get; }

    public StoreCatalogSearchCandidatePortFake SearchCandidate { get; }

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage => pump.SentMessages<TMessage>();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent => discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent => discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static GetTracksForPlaylistSociableTestEnvironment ForNoExistingDataOrRequests() =>
        Compose(default, []);

    public static GetTracksForPlaylistSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingIncompleteLookup() =>
        ForExistingIncompleteLookup(default);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataNotComplete() =>
        Compose(default, []);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataNotComplete(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime)
    {
        var environment = Compose(requestTime, []);
        await environment.sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId));
        await environment.pump.PumpNextMessageAsync();
        return environment;
    }

    public static Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteTrack[] tracks) =>
        ForExistingCompletedLookup(default, tracks);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteTrack[] tracks) =>
        Compose(default, tracks);

    public static GetTracksForPlaylistSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteTrack[] tracks) =>
        Compose(requestTime, tracks);

    public static async Task<GetTracksForPlaylistSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteTrack[] tracks)
    {
        var environment = Compose(requestTime, tracks);

        await environment.ProjectOnChange(
            subject => subject.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        return environment;
    }

    public Task<TResult> ProjectOnChange<TResult>(Func<GetTracksForPlaylistHandler, Task<TResult>> change) =>
        pump.ProjectOnChange(change, sut);

    public void Dispose() => engine.Dispose();

    private static GetTracksForPlaylistSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteTrack> completedTracks)
    {
        var engine = SociableDiscoveryEngine.Create(utcNow);
        var options = engine.Resolve<SociableScenarioOptions>();

        SeedLookupData(
            options.PlaylistId,
            completedTracks,
            RequireFake<IReadPlaylistTracksByProviderPort, ReadPlaylistTracksByProviderPortFake>(engine),
            RequireFake<IReadCatalogEntriesBySearchCriteriaPort, ReadCatalogEntriesBySearchCriteriaPortFake>(engine),
            RequireFake<IReadStreamingLocationByProviderPort, ReadStreamingLocationByProviderPortFake>(engine));

        var sut = engine.Resolve<IApiHandler<GetTracksForPlaylistRequest, GetTracksForPlaylistResponse?>>()
            as GetTracksForPlaylistHandler
            ?? throw new InvalidOperationException("GetTracksForPlaylistHandler was not resolved from sociable discovery.");

        return new GetTracksForPlaylistSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            options.PlaylistId,
            RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(engine),
            RequireFake<IStorePlaylistTracksReadModelPort, StorePlaylistTracksReadModelPortFake>(engine),
            RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>(engine),
            RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(engine),
            RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>(engine));
    }

    private static TFake RequireFake<TService, TFake>(SociableDiscoveryEngine engine)
        where TService : class
        where TFake : class, TService =>
        engine.Resolve<TService>() as TFake
        ?? throw new InvalidOperationException(
            $"Expected '{typeof(TService).Name}' to be '{typeof(TFake).Name}'.");

    private static void SeedLookupData(
        PlaylistId playlistId,
        IReadOnlyList<LookupDataCompleteTrack> completedTracks,
        ReadPlaylistTracksByProviderPortFake readPlaylistTracks,
        ReadCatalogEntriesBySearchCriteriaPortFake readCatalogEntries,
        ReadStreamingLocationByProviderPortFake readStreamingLocation)
    {
        readPlaylistTracks.WithTracks(
            playlistId,
            ProviderName.Spotify,
            completedTracks.Select(static track => track.PlaylistTrack).ToArray());

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
    }
}
