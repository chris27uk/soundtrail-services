using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.MusicTracks;
using System.Text.Json;

namespace Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IMusicTrackEventRepository musicTrackEventRepository,
    IMusicTrackProjectionStore musicTrackProjectionStore,
    IProviderSnapshotStore providerSnapshotStore)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var musicTrack = await MusicTrack.LoadAsync(
            response.MusicCatalogId,
            musicTrackEventRepository,
            cancellationToken);
        musicTrack.Record(response);
        var append = await musicTrack.SaveAsync(
            musicTrackEventRepository,
            response.CommandId,
            cancellationToken);

        if (!append.Appended)
        {
            return EnrichmentOrchestrationResult.Empty();
        }

        await providerSnapshotStore.SaveAsync(
            new ProviderSnapshot(
                response.MusicCatalogId,
                response.SourceProvider,
                response.CreatedAt,
                response.RawPayloadJson ?? JsonSerializer.Serialize(response)),
            cancellationToken);

        await musicTrackProjectionStore.StoreAsync(response.MusicCatalogId, musicTrack, cancellationToken);
        return new EnrichmentOrchestrationResult(append.AppendedFacts);
    }
}
