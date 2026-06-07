using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

namespace Soundtrail.Services.Enrichment.Features.Orchestration;

public sealed record EnrichmentOrchestrationResult(IReadOnlyList<MusicTrackFact> Facts)
{
    public static EnrichmentOrchestrationResult Empty() => new([]);
}
