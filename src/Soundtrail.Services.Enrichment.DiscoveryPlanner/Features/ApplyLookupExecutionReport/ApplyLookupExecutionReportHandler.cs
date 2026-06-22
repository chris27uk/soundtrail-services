using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Support;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport;

public sealed class ApplyLookupExecutionReportHandler(
    CatalogSearchDiscoveryByMusicCatalogIdTransitionApplier transitionApplier)
{
    public Task Handle(
        LookupExecutionReportDto report,
        CancellationToken cancellationToken = default)
    {
        var outcome = Enum.Parse<LookupExecutionOutcome>(report.Outcome);
        return outcome switch
        {
            LookupExecutionOutcome.Deferred => transitionApplier.ApplyAsync(
                MusicCatalogId.From(report.MusicCatalogId),
                discovery => discovery.Defer(
                    report.RetryAfterSeconds,
                    report.RetryAt,
                    report.Reason ?? "Lookup deferred",
                    report.CreatedAt),
                cancellationToken),
            LookupExecutionOutcome.Failed => transitionApplier.ApplyAsync(
                MusicCatalogId.From(report.MusicCatalogId),
                discovery => discovery.Fail(
                    report.Reason ?? "Lookup failed",
                    report.CreatedAt),
                cancellationToken),
            _ => Task.CompletedTask
        };
    }
}
