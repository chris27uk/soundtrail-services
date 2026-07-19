using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Extensions;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Shared;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired;

public sealed class OnMusicAssessmentRequiredHandler(
    IPlanningAssessmentPolicy policy,
    IDiscoveryPlanningProjectionReader projectionReader,
    IEventStreamRepository<CatalogWorkId> repository) : IHandler<AssessWorkCommand>
{
    public async Task Handle(AssessWorkCommand request, CancellationToken cancellationToken = default)
    {
        var context = request.ToAggregateContext();
        var streamId = CatalogWorkId.From(request.Target);
        var projection = await projectionReader.ReadAsync(request.Target, cancellationToken);
        var assessment = policy.Evaluate(request.ToPlanningAssessment(projection));
        await using var scope = await DiscoveryHistoryScope.LoadFromEventStreamAsync(repository, streamId, context, cancellationToken);

        scope.Aggregate
            .Assess(assessment)
            .IgnoreCompletedWork()
            .RejectPreviouslyRejectedWork()
            .IgnoreDuplicateWork()
            .DeferWhenHighPriorityCapacityIsProtected()
            .DeferWhenPlannerCapacityIsFull()
            .ScheduleOtherwise();

        scope.Save();
    }
}
