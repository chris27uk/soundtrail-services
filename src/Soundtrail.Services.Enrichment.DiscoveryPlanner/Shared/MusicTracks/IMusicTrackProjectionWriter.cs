using Soundtrail.Contracts;
using Soundtrail.Contracts.Orchestrator;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

public interface IMusicTrackProjectionWriter
{
    void WriteCanonicalMetadata(
        string title,
        string artist,
        string? isrc,
        string? mbid,
        int? durationMs);

    void WriteProviderReference(
        ProviderName provider,
        Uri url,
        string? externalId,
        ReferenceConfidenceDto confidenceDto,
        ProviderName sourceProvider);

    void WritePlayable(bool isPlayable);
}
