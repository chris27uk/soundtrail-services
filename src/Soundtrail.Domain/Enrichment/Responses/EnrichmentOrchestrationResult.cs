using Soundtrail.Domain.Events;

namespace Soundtrail.Domain.Responses
{
    public sealed record EnrichmentOrchestrationResult(IReadOnlyList<IMusicTrackEvent> Events);
}
