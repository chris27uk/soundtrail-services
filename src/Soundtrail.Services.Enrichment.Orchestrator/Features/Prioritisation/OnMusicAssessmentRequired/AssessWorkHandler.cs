using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Shared;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;

public sealed class OnMusicAssessmentRequiredHandler(
    IPlanningAssessmentPolicy policy,
    IDiscoveryPlanningProjectionReader projectionReader,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<AssessWorkMessage>
{
    public async Task Handle(IncomingMessage<AssessWorkMessage> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        using var handlerActivity = MessageTelemetry.StartHandlerActivity(request, "assess-work");
        MessageTelemetry.EnrichCurrentActivity(request, "assess-work");
        MessageTelemetry.AddCurrentEvent("assess-work.received");

        var aggregateContext = request.ToAggregateContext();
        var streamId = CatalogWorkId.From(request.Target);
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, aggregateContext, cancellationToken);
        var projection = await projectionReader.ReadAsync(request.Target, cancellationToken);
        var demand = scope.Aggregate.GetDemandState(request.Target);
        var assessment = policy.Evaluate(request.ToPlanningAssessment(projection, demand));
        MessageTelemetry.AddCurrentEvent("assess-work.evaluated");

        scope.Aggregate
            .Assess(assessment)
            .IgnoreCompletedWork()
            .RejectPreviouslyRejectedWork()
            .IgnoreDuplicateWork()
            .DeferWhenHighPriorityCapacityIsProtected()
            .DeferWhenPlannerCapacityIsFull()
            .ScheduleOtherwise();

        scope.Save();
        MessageTelemetry.AddCurrentEvent("assess-work.saved");
    }
}
