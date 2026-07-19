using Soundtrail.Domain.Discovery.Assesment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;

public interface IPlanningAssessmentPolicy
{
    PlanningAssessment Evaluate(PlanningAssessment assessment);
}
