using Soundtrail.Services.Enrichment.Features.Orchestration;
using System.Text.Json;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

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
        var stream = await musicTrackEventRepository.LoadEventsAsync(
            response.MusicCatalogId,
            cancellationToken);

        var facts = MusicTrackFactBuilder.Build(stream, response);
        var append = await musicTrackEventRepository.AppendEventsAsync(
            response.MusicCatalogId,
            stream.Version,
            response.CommandId,
            facts,
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
                JsonSerializer.Serialize(response)),
            cancellationToken);

        var updatedStream = new MusicTrackStream(
            append.Version,
            stream.Facts.Concat(append.AppendedFacts).ToArray());

        await musicTrackProjectionStore.StoreAsync(response.MusicCatalogId, updatedStream, cancellationToken);
        return new EnrichmentOrchestrationResult(append.AppendedFacts);
    }
}
