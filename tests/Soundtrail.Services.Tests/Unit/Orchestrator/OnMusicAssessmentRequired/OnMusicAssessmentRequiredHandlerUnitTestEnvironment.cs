using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnMusicAssessmentRequired;

internal sealed class OnMusicAssessmentRequiredHandlerUnitTestEnvironment
{
    private OnMusicAssessmentRequiredHandlerUnitTestEnvironment(
        EventStreamRepositoryFake repository,
        DiscoveryPlanningProjectionReaderFake projectionReader,
        IPlanningAssessmentPolicy policy)
    {
        Repository = repository;
        ProjectionReader = projectionReader;
        Policy = policy;
    }

    public EventStreamRepositoryFake Repository { get; }

    public DiscoveryPlanningProjectionReaderFake ProjectionReader { get; }

    public IPlanningAssessmentPolicy Policy { get; }

    public static OnMusicAssessmentRequiredHandlerUnitTestEnvironment Create(PlanningAssessmentOptions? options = null)
    {
        var repository = new EventStreamRepositoryFake();
        var projectionReader = new DiscoveryPlanningProjectionReaderFake();
        var policy = new PlanningAssessmentPolicy(Options.Create(options ?? new PlanningAssessmentOptions()));
        return new(repository, projectionReader, policy);
    }

    public OnMusicAssessmentRequiredHandler CreateSubject() => new(Policy, ProjectionReader, Repository);

    public static AssessWorkCommand CreateRequest(
        EnrichmentTarget? target = null,
        LookupPriorityBand priority = LookupPriorityBand.High,
        int? trustLevel = 100,
        int? riskScore = 0,
        DateTimeOffset? createdAt = null,
        string commandId = "assess-1",
        string correlationId = "corr-1") =>
        new(
            CommandId.For(commandId),
            CorrelationId.From(correlationId),
            createdAt ?? new DateTimeOffset(2026, 7, 18, 9, 30, 0, TimeSpan.Zero),
            target ?? Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-123")),
            priority,
            trustLevel,
            riskScore);

    public sealed class DiscoveryPlanningProjectionReaderFake : IDiscoveryPlanningProjectionReader
    {
        public DiscoveryPlanningProjection ProjectionToReturn { get; set; } = new(false, null, 0, 0);

        public Task<DiscoveryPlanningProjection> ReadAsync(
            EnrichmentTarget target,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProjectionToReturn);
    }

    public sealed class EventStreamRepositoryFake : IEventStreamRepository<CatalogWorkId>
    {
        public LoadedEventStream<CatalogWorkId>? LoadedStream { get; private set; }

        public IReadOnlyList<IDomainEvent> SeedEvents { get; set; } = [];

        public IReadOnlyList<IDomainEvent> AppendedEvents { get; private set; } = [];

        public Task<LoadedEventStream<CatalogWorkId>> LoadAsync(
            CatalogWorkId streamId,
            CancellationToken cancellationToken)
        {
            LoadedStream = SeedEvents.Count == 0
                ? LoadedEventStream<CatalogWorkId>.Empty(streamId)
                : new LoadedEventStream<CatalogWorkId>(streamId, SeedEvents.Count, SeedEvents);

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
