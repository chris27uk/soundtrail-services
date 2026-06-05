using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

internal sealed class RavenAppliedEnrichmentResponseStore(
    IAsyncDocumentSession session) : IAppliedEnrichmentResponseStore
{
    public async Task<bool> HasAppliedAsync(CommandId commandId, CancellationToken cancellationToken)
    {
        var documentId = RavenAppliedEnrichmentResponseDocument.GetDocumentId(commandId.Value);
        return await session.LoadAsync<RavenAppliedEnrichmentResponseDocument>(documentId, cancellationToken) is not null;
    }

    public Task MarkAppliedAsync(CommandId commandId, CancellationToken cancellationToken) =>
        session.StoreAsync(
            new RavenAppliedEnrichmentResponseDocument
            {
                Id = RavenAppliedEnrichmentResponseDocument.GetDocumentId(commandId.Value),
                CommandId = commandId.Value
            },
            cancellationToken);
}
