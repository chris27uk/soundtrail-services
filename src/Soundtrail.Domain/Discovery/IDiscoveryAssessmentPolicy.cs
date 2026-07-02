namespace Soundtrail.Domain.Discovery;

public interface IDiscoveryAssessmentPolicy
{
    DiscoveryAssessment Assess(
        DiscoveryAssessmentSummary summary,
        DateTimeOffset now);
}
