using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.ApplyLookupExecutionReport;

public sealed class ApplyLookupExecutionReportHandler(
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
{
    public async Task Handle(
        LookupExecutionReportDto report,
        CancellationToken cancellationToken = default)
    {
        var outcome = Enum.Parse<LookupExecutionOutcome>(report.Outcome);
        if (outcome is not (LookupExecutionOutcome.Deferred or LookupExecutionOutcome.Failed))
        {
            return;
        }

        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(
            MusicCatalogId.From(report.MusicCatalogId),
            cancellationToken);

        foreach (var tracking in trackings)
        {
            var discovery = await CatalogSearchDiscovery.LoadAsync(
                discoveryRepository,
                tracking.Criteria,
                cancellationToken);

            var changed = outcome switch
            {
                LookupExecutionOutcome.Deferred => discovery.Defer(
                    report.RetryAfterSeconds,
                    report.RetryAt,
                    report.Reason ?? "Lookup deferred",
                    report.CreatedAt),
                LookupExecutionOutcome.Failed => discovery.Fail(
                    report.Reason ?? "Lookup failed",
                    report.CreatedAt),
                _ => false
            };

            if (!changed)
            {
                continue;
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }
}
