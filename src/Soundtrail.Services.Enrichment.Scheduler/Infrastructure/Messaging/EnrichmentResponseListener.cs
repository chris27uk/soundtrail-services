using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class EnrichmentResponseListener(ApplyEnrichmentResponseHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        EnrichmentResponse response,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default) =>
        handler.Handle(response, cancellationToken);
}
