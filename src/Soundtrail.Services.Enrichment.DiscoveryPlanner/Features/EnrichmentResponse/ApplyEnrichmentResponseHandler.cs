using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Support;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse;

public sealed class ApplyEnrichmentResponseHandler(
    AppendCatalogEnrichmentResponse appendCatalogEnrichmentResponse,
    CaptureProviderSnapshot captureProviderSnapshot,
    ProjectCatalogSearchTrackings projectCatalogSearchTrackings,
    CompleteTrackedDiscoveries completeTrackedDiscoveries)
{
    public async Task<EnrichmentOrchestrationResult> Handle(
        Domain.Responses.EnrichmentResponse response,
        CancellationToken cancellationToken = default)
    {
        var events = await appendCatalogEnrichmentResponse.AppendAsync(response, cancellationToken);
        await captureProviderSnapshot.CaptureAsync(response, cancellationToken);
        await projectCatalogSearchTrackings.ProjectAsync(response, cancellationToken);
        await completeTrackedDiscoveries.CompleteAsync(response, cancellationToken);

        return new EnrichmentOrchestrationResult(events);
    }
}
