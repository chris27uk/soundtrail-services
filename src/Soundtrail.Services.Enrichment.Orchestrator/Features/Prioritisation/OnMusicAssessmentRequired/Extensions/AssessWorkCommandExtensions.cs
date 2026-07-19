using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Extensions
{
    public static class AssessWorkCommandExtensions
    {
        public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this ICommand request)
        {
            return new DiscoveryHistory.SearchRequestContext(
                request.CommandId,
                request is IPrioritisedCommand prioritised ? prioritised.TrustLevel ?? 0 : 0,
                request is IPrioritisedCommand prioritisedRequest ? prioritisedRequest.RiskScore ?? 0 : 0,
                request.RequestedAt,
                request.CorrelationId);
        }

        public static PlanningAssessment ToPlanningAssessment(this AssessWorkCommand request, DiscoveryPlanningProjection projection)
        {
            return new PlanningAssessment(
                request.Target,
                request.Priority,
                request.RequestedAt,
                request.TrustLevel,
                request.RiskScore,
                projection);
        }
    }
}
