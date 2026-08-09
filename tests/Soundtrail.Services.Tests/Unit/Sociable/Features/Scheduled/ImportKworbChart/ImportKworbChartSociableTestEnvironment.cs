using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Operations;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.ImportKworbChart;

internal sealed class ImportKworbChartSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly IScheduledMessageHandler<ImportKworbChartCommand> sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private ImportKworbChartSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        IScheduledMessageHandler<ImportKworbChartCommand> sut,
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

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => this.pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage => this.pump.SentMessages<TMessage>();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent =>
        this.discoveryRepository.SavedEvents.Concat(this.artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent =>
        this.discoveryRepository.SavedEvents.Concat(this.artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static ImportKworbChartSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset utcNow = default) =>
        Compose(utcNow, []);

    public static ImportKworbChartSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset utcNow,
        params LookupDataCompleteTrack[] tracks) =>
        Compose(utcNow, tracks);

    public Task ProjectOnChange(Func<IScheduledMessageHandler<ImportKworbChartCommand>, Task> change) =>
        this.pump.ProjectOnChange(
            async subject =>
            {
                await change(subject);
                return true;
            },
            this.sut);

    public Task TriggerImportAsync(DateTimeOffset triggeredAt) =>
        ProjectOnChange(subject => subject.HandleAsync(new ImportKworbChartCommand(triggeredAt)));

    public void Dispose() => this.engine.Dispose();

    private static ImportKworbChartSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteTrack> completedTracks)
    {
        var engine = SociableDiscoveryEngine.Create(utcNow);
        var playlistId = PlaylistId.FromPlaylistName("WorldwideSongChart");

        SeedLookupData(
            playlistId,
            completedTracks,
            engine.RequireFake<IReadPlaylistTracksByProviderPort, ReadPlaylistTracksByProviderPortFake>(),
            engine.RequireFake<IReadCatalogEntriesBySearchCriteriaPort, ReadCatalogEntriesBySearchCriteriaPortFake>(),
            engine.RequireFake<IReadStreamingLocationByProviderPort, ReadStreamingLocationByProviderPortFake>());

        var sut = engine.Resolve<IScheduledMessageHandler<ImportKworbChartCommand>>();

        return new ImportKworbChartSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            playlistId,
            engine.RequireFake<IStoreDiscoveryFeedbackPort, StoreDiscoveryFeedbackPortFake>(),
            engine.RequireFake<IStorePlaylistTracksReadModelPort, StorePlaylistTracksReadModelPortFake>(),
            engine.RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>(),
            engine.RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(),
            engine.RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>());
    }

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
