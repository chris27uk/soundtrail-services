using Raven.Client.Documents.Session;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Adapters;

public sealed class LookupExecutionReportListener(ApplyLookupExecutionReportHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(LookupExecutionReportDto dto, IAsyncDocumentSession _, CancellationToken cancellationToken = default) => handler.Handle(dto, cancellationToken);
}
