using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

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

    public static AssessWorkMessage CreateRequest(
        EnrichmentTarget? target = null,
        LookupPriorityBand priority = LookupPriorityBand.High,
        int? trustLevel = 100,
        int? riskScore = 0,
        DateTimeOffset? createdAt = null,
        string commandId = "assess-1",
        string correlationId = "corr-1") =>
        new(
            MessageId.For(commandId),
            CorrelationId.From(correlationId),
            createdAt ?? new DateTimeOffset(2026, 7, 18, 9, 30, 0, TimeSpan.Zero),
            target ?? Work.EnrichTrackStreamingLocation(TestTrackIds.Create("track-123")),
            priority,
            trustLevel,
            riskScore);

}
