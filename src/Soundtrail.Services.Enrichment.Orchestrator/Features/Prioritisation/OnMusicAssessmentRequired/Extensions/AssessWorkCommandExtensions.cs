using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Extensions
{
    public static class AssessWorkCommandExtensions
    {
        public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this IMessage request)
        {
            return new DiscoveryHistory.SearchRequestContext(
                request.Id,
                request is IPrioritisedMessage prioritised ? prioritised.TrustLevel ?? 0 : 0,
                request is IPrioritisedMessage prioritisedRequest ? prioritisedRequest.RiskScore ?? 0 : 0,
                request.RequestedAt,
                request.CorrelationId);
        }

        public static PlanningAssessment ToPlanningAssessment(
            this AssessWorkMessage request,
            DiscoveryPlanningProjection projection,
            DiscoveryHistory.WorkDemandState? demand)
        {
            return new PlanningAssessment(
                request.Target,
                demand?.Priority ?? request.Priority,
                demand?.RequestedAt ?? request.RequestedAt,
                demand?.TrustLevel ?? request.TrustLevel,
                demand?.RiskScore ?? request.RiskScore,
                projection);
        }
    }
}
