using Soundtrail.Services.Enrichment.Features.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IAppliedEnrichmentResponseStore appliedEnrichmentResponseStore,
    ITrackEnrichmentWriteStore trackEnrichmentWriteStore,
    EnrichmentOrchestrator enrichmentOrchestrator)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        if (await appliedEnrichmentResponseStore.HasAppliedAsync(response.CommandId, cancellationToken))
        {
            return EnrichmentOrchestrationResult.Empty();
        }

        TrackEnrichmentState? previousState = null;
        TrackEnrichmentState? updatedState = null;

        await trackEnrichmentWriteStore.ApplyAsync(
            response.MusicCatalogId,
            state =>
            {
                previousState = state.Copy();

                if (response.SourceProvider == ProviderName.MusicBrainz && response.Metadata is not null)
                {
                    state.ApplyCanonicalMetadata(response.Metadata);
                }

                foreach (var reference in response.References)
                {
                    state.ApplyReference(
                        reference.Provider,
                        reference.Url,
                        reference.ExternalId,
                        reference.Confidence,
                        response.SourceProvider);
                }

                updatedState = state.Copy();
            },
            cancellationToken);

        await appliedEnrichmentResponseStore.MarkAppliedAsync(response.CommandId, cancellationToken);
        return await enrichmentOrchestrator.PlanAsync(previousState!, updatedState!, response, cancellationToken);
    }
}
