using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

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
        ReferenceConfidence confidence,
        ProviderName sourceProvider);

    void WritePlayable(bool isPlayable);
}
