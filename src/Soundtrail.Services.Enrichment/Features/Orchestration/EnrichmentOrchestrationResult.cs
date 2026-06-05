using Soundtrail.Services.Enrichment.Shared.Orchestration;

namespace Soundtrail.Services.Enrichment.Features.Orchestration;

public sealed record EnrichmentOrchestrationResult(
    IReadOnlyList<IEnrichmentIntentCommand> Commands,
    IReadOnlyList<IEnrichmentOrchestrationEvent> Events)
{
    public static EnrichmentOrchestrationResult Empty() => new([], []);
}
