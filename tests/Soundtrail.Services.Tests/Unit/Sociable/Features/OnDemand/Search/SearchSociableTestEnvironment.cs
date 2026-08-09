using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search;

internal sealed class SearchSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly SearchHandler sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private SearchSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        SearchHandler sut,
        SearchCriteria searchCriteria,
        InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository,
        InMemoryEventStreamRepository<ArtistId> artistRepository)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        SearchCriteria = searchCriteria;
        this.discoveryRepository = discoveryRepository;
        this.artistRepository = artistRepository;
    }

    public SearchCriteria SearchCriteria { get; }

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => this.pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage => this.pump.SentMessages<TMessage>();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent =>
        this.discoveryRepository.SavedEvents.Concat(this.artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent =>
        this.discoveryRepository.SavedEvents.Concat(this.artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static SearchSociableTestEnvironment ForNoExistingDataOrRequests() =>
        Compose(default, LookupDataCompleteSearchScenarios.DefaultCriteria, []);

    public static SearchSociableTestEnvironment ForNoExistingDataOrRequests(DateTimeOffset requestTime) =>
        Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, []);

    public static SearchSociableTestEnvironment ForLookupDataNotComplete() =>
        Compose(default, LookupDataCompleteSearchScenarios.DefaultCriteria, []);

    public static SearchSociableTestEnvironment ForLookupDataNotComplete(DateTimeOffset requestTime) =>
        Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, []);

    public static async Task<SearchSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, []);
        await environment.sut.Handle(environment.CreateRequest());
        await environment.pump.PumpNextMessageAsync();
        return environment;
    }

    public static SearchSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteSearchArtist[] artists) =>
        Compose(default, LookupDataCompleteSearchScenarios.DefaultCriteria, artists);

    public static SearchSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteSearchArtist[] artists) =>
        Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, artists);

    public static SearchSociableTestEnvironment ForNoResultsFound() =>
        Compose(default, LookupDataCompleteSearchScenarios.DefaultCriteria, []);

    public static SearchSociableTestEnvironment ForLocalTrackCandidate(
        TrackId? trackId = null,
        string query = "Aurora Lane") =>
        ComposeWithLocalCandidate(
            new SearchCriteria(query, SearchType.Track),
            new CatalogSearchCandidateProjection(
                (trackId ?? TrackId.From(TestTrackIds.Value("track-123"))).Value,
                "track",
                query,
                query,
                null,
                null,
                null,
                default));

    public static SearchSociableTestEnvironment ForLocalArtistCandidate(
        ArtistId? artistId = null,
        string query = "Aurora Lane") =>
        ComposeWithLocalCandidate(
            new SearchCriteria(query, SearchType.Artist),
            new CatalogSearchCandidateProjection(
                (artistId ?? ArtistId.From("artist-123")).Value,
                "artist",
                query,
                query,
                null,
                null,
                null,
                default));

    public static SearchSociableTestEnvironment ForLocalAlbumCandidate(
        AlbumId? albumId = null,
        string query = "Aurora Lane") =>
        ComposeWithLocalCandidate(
            new SearchCriteria(query, SearchType.Album),
            new CatalogSearchCandidateProjection(
                (albumId ?? AlbumId.From("artist-123", "album-123")).StableValue,
                "album",
                query,
                query,
                null,
                query,
                null,
                default));

    public static SearchSociableTestEnvironment ForLocalPlaylistCandidate(
        string playlistName = "road trip",
        string query = "Aurora Lane") =>
        ComposeWithLocalCandidate(
            new SearchCriteria(query, SearchType.All),
            new CatalogSearchCandidateProjection(
                PlaylistId.FromPlaylistName(playlistName).Value,
                "playlist",
                query,
                playlistName,
                null,
                null,
                null,
                default));

    public static async Task<SearchSociableTestEnvironment> ForExistingCompletedEmptyLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, []);
        await environment.ProjectOnChange(subject => subject.Handle(environment.CreateRequest()));
        return environment;
    }

    public static async Task<SearchSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteSearchArtist[] artists) =>
        await ForExistingCompletedLookup(default, artists);

    public static async Task<SearchSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteSearchArtist[] artists)
    {
        var environment = Compose(requestTime, LookupDataCompleteSearchScenarios.DefaultCriteria, artists);
        await environment.ProjectOnChange(subject => subject.Handle(environment.CreateRequest()));
        return environment;
    }

    public Task<TResult> ProjectOnChange<TResult>(Func<SearchHandler, Task<TResult>> change) =>
        this.pump.ProjectOnChange(change, this.sut);

    public SearchRequest CreateRequest() => new(SearchCriteria.Query, SearchCriteria.SearchTypes);

    public void Dispose() => this.engine.Dispose();

    private static SearchSociableTestEnvironment ComposeWithLocalCandidate(
        SearchCriteria searchCriteria,
        CatalogSearchCandidateProjection candidate)
    {
        var environment = Compose(default, searchCriteria, []);
        environment.engine
            .RequireFake<IStoreCatalogSearchCandidatePort, StoreCatalogSearchCandidatePortFake>()
            .StoreAsync(candidate, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return environment;
    }

    private static SearchSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        SearchCriteria searchCriteria,
        IReadOnlyList<LookupDataCompleteSearchArtist> completedArtists)
    {
        var engine = SociableDiscoveryEngine.Create(utcNow);

        engine.RequireFake<IReadCatalogEntriesBySearchCriteriaPort, ReadCatalogEntriesBySearchCriteriaPortFake>()
            .WithEntries(
                searchCriteria,
                completedArtists.Select(static artist => artist.CatalogEntry).ToArray());

        var sut = engine.Resolve<IApiHandler<SearchRequest, SearchResponse?>>() as SearchHandler
            ?? throw new InvalidOperationException("SearchHandler was not resolved from sociable discovery.");

        return new SearchSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            searchCriteria,
            engine.RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(),
            engine.RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>());
    }
}
