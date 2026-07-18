using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Assesment;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicAssessmentRequired.Planning;

public interface IDiscoveryPlanningProjectionReader
{
    Task<DiscoveryPlanningProjection> ReadAsync(
        EnrichmentTarget target,
        CancellationToken cancellationToken);
}
