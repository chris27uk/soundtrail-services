namespace Soundtrail.Domain.Discovery;

public interface ICatalogDiscoveryWorkPlanningPolicy
{
    CatalogDiscoveryWorkAssessment Assess(
        CatalogDiscoveryWorkSummary summary,
        DateTimeOffset now);
}
