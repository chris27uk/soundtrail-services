using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Domain.Enrichment.Responses
{
    public sealed record EnrichmentOrchestrationResult(IReadOnlyList<IMusicTrackEvent> Events);
}
