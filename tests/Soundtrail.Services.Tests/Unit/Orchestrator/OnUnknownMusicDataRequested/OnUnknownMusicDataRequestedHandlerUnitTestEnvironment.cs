using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnUnknownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnUnknownMusicDataRequested;

internal sealed class OnUnknownMusicDataRequestedHandlerUnitTestEnvironment
{
    private OnUnknownMusicDataRequestedHandlerUnitTestEnvironment(
        SearchForCandidatesFake searchForCandidates,
        EventStreamRepositoryFake repository)
    {
        SearchForCandidates = searchForCandidates;
        Repository = repository;
    }

    public SearchForCandidatesFake SearchForCandidates { get; }

    public EventStreamRepositoryFake Repository { get; }

    public static OnUnknownMusicDataRequestedHandlerUnitTestEnvironment Create() =>
        new(new SearchForCandidatesFake(), new EventStreamRepositoryFake());

    public OnUnknownMusicDataRequestedHandler CreateSubject() => new(new WorkPlanner(), SearchForCandidates, Repository);

    public static RequestUnknownMusicDataCommand CreateUnknownRequest(
        string query = "radiohead",
        SearchType searchType = SearchType.Artist,
        LookupPriorityBand priority = LookupPriorityBand.High,
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-unknown",
        string correlationId = "corr-unknown") =>
        new(
            new SearchCriteria(query, searchType),
            priority,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            CommandId = CommandId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

    public static CandidatesResult CreateTrackCandidates(string trackId = "track-123") =>
        new CandidatesResult.Results(CandidateList.From([
            new ScoredCandidate(new CatalogItemId.Track(TrackId.From(trackId)), 900)
        ]));

    public static CandidatesResult CreateArtistCandidates(string artistId = "artist-123") =>
        new CandidatesResult.Results(CandidateList.From([
            new ScoredCandidate(new CatalogItemId.Artist(ArtistId.From(artistId)), 900)
        ]));

    public static CandidatesResult CreateAlbumCandidates(string artistId = "artist-123", string albumId = "album-123") =>
        new CandidatesResult.Results(CandidateList.From([
            new ScoredCandidate(new CatalogItemId.Album(AlbumId.From(artistId, albumId)), 900)
        ]));

    public static CandidatesResult CreatePlaylistCandidates(string playlistName = "road trip") =>
        new CandidatesResult.Results(CandidateList.From([
            new ScoredCandidate(new CatalogItemId.Playlist(PlaylistId.FromPlaylistName(playlistName)), 900)
        ]));

    public sealed class SearchForCandidatesFake : ISearchForCandidates
    {
        public int Calls { get; private set; }

        public EnrichmentTarget? LastTarget { get; private set; }

        public CandidatesResult ResultToReturn { get; set; } = new CandidatesResult.None();

        public CandidatesResult Search(EnrichmentTarget target)
        {
            Calls++;
            LastTarget = target;
            return ResultToReturn;
        }
    }

    public sealed class EventStreamRepositoryFake : IEventStreamRepository<CatalogWorkId>
    {
        public LoadedEventStream<CatalogWorkId>? LoadedStream { get; private set; }

        public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

        public Task<LoadedEventStream<CatalogWorkId>> LoadAsync(
            CatalogWorkId streamId,
            CancellationToken cancellationToken)
        {
            LoadedStream = LoadedEventStream<CatalogWorkId>.Empty(streamId);
            return Task.FromResult(LoadedStream);
        }

        public Task<AppendResult> AppendAsync(
            LoadedEventStream<CatalogWorkId> stream,
            IReadOnlyList<IDomainEvent> events,
            OperationId? operationId,
            CancellationToken cancellationToken)
        {
            AppendedEvents = events.ToArray();
            return Task.FromResult(new AppendResult(true, stream.Version + events.Count, events.ToArray(), AppendOutcome.Appended));
        }
    }
}
