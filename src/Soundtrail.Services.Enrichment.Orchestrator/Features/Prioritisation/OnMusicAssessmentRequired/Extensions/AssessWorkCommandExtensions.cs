using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Extensions
{
    public static class AssessWorkCommandExtensions
    {
        public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this AssessWorkCommand request)
        {
            return new DiscoveryHistory.SearchRequestContext(
                request.TrustLevel ?? 0,
                request.RiskScore ?? 0,
                request.CreatedAt,
                request.CorrelationId);
        }
        
        public static PlanningAssessment ToPlanningAssessment(this AssessWorkCommand request, DiscoveryPlanningProjection projection)
        {
            return new PlanningAssessment(
                request.Target,
                request.Priority,
                request.CreatedAt,
                request.TrustLevel,
                request.RiskScore,
                projection);
        }
    }
}
