using Soundtrail.Services.Enrichment.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    IAppliedEnrichmentResponseStore appliedEnrichmentResponseStore,
    ITrackEnrichmentWriteStore trackEnrichmentWriteStore,
    IFollowUpEnrichmentScheduler followUpEnrichmentScheduler)
{
    public async Task Handle(
        EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        if (await appliedEnrichmentResponseStore.HasAppliedAsync(response.CommandId, cancellationToken))
        {
            return;
        }

        TrackEnrichmentState? updatedState = null;

        await trackEnrichmentWriteStore.ApplyAsync(
            response.MusicCatalogId,
            state =>
            {
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

                updatedState = state;
            },
            cancellationToken);

        await appliedEnrichmentResponseStore.MarkAppliedAsync(response.CommandId, cancellationToken);
        await followUpEnrichmentScheduler.ScheduleAsync(updatedState!, response, cancellationToken);
    }
}
