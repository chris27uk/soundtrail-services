using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Candidates;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownMusicDataRequested;
using Soundtrail.Services.Enrichment.Orchestrator.Features.RequestedWork;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnKnownMusicDataRequested;

internal sealed class OnKnownMusicDataRequestedHandlerUnitTestEnvironment
{
    private OnKnownMusicDataRequestedHandlerUnitTestEnvironment(
        SearchForCandidatesFake searchForCandidates,
        EventStreamRepositoryFake repository)
    {
        SearchForCandidates = searchForCandidates;
        Repository = repository;
    }

    public SearchForCandidatesFake SearchForCandidates { get; }

    public EventStreamRepositoryFake Repository { get; }

    public static OnKnownMusicDataRequestedHandlerUnitTestEnvironment Create() =>
        new(new SearchForCandidatesFake(), new EventStreamRepositoryFake());

    public OnKnownMusicDataRequestedHandler CreateSubject() => new(new WorkPlanner(), Repository);

    public static RequestKnownMusicDataCommand CreateKnownArtistRequest(
        string artistId = "artist-123",
        int trustLevel = 100,
        int riskScore = 0,
        DateTimeOffset? requestedAt = null,
        string commandId = "cmd-1",
        string correlationId = "corr-1") =>
        new(
            new CatalogItemOperation.ChildTracksForArtist(ArtistId.From(artistId)),
            LookupPriorityBand.High,
            trustLevel,
            riskScore,
            requestedAt ?? new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero))
        {
            CommandId = CommandId.For(commandId),
            CorrelationId = CorrelationId.From(correlationId)
        };

    public sealed class SearchForCandidatesFake : ISearchForCandidates
    {
        public int Calls { get; private set; }

        public CandidatesResult ResultToReturn { get; set; } = new CandidatesResult.None();

        public CandidatesResult Search(EnrichmentTarget target)
        {
            Calls++;
            return ResultToReturn;
        }
    }

    public sealed class EventStreamRepositoryFake : IEventStreamRepository<CatalogWorkId>
    {
        public LoadedEventStream<CatalogWorkId>? LoadedStream { get; private set; }

        public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

        public OperationId? LastOperationId { get; private set; }

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
            LastOperationId = operationId;
            return Task.FromResult(new AppendResult(true, stream.Version + events.Count, events.ToArray(), AppendOutcome.Appended));
        }
    }
}
