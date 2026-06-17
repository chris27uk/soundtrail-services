using Soundtrail.Domain.Events;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.ApplyResponse
{
    public sealed record EnrichmentOrchestrationResult(IReadOnlyList<IMusicTrackEvent> Events);
}