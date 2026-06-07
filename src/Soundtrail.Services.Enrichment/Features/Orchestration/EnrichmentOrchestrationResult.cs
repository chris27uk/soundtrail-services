using Soundtrail.Services.Enrichment.Shared.MusicTracks;

namespace Soundtrail.Services.Enrichment.Features.Orchestration;

public sealed record EnrichmentOrchestrationResult(IReadOnlyList<MusicTrackFact> Facts)
{
    public static EnrichmentOrchestrationResult Empty() => new([]);
}
