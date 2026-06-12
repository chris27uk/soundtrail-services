using Soundtrail.Domain.Events;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse
{
    public sealed record EnrichmentOrchestrationResult(IReadOnlyList<IMusicTrackEvent> Events);
}