using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
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
                MusicCatalogId.From(response.MusicCatalogId),
                ProviderName.From(response.SourceProvider),
                response.CreatedAt,
                JsonSerializer.Serialize(response)),
            cancellationToken);

        await musicTrackProjectionStore.StoreAsync(MusicCatalogId.From(response.MusicCatalogId), musicTrack, cancellationToken);
        return new EnrichmentOrchestrationResult(append.AppendedFacts);
    }
}
