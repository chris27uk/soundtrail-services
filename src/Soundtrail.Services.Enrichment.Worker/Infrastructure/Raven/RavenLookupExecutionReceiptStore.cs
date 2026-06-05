using Raven.Client.Documents.Session;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

internal sealed class RavenLookupExecutionReceiptStore(
    IAsyncDocumentSession session) : ILookupExecutionReceiptStore
{
    public async Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDocument.GetDocumentId(commandId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDocument>(documentId, cancellationToken);
        if (existing is not null)
        {
            return false;
        }

        await session.StoreAsync(
            new RavenLookupExecutionReceiptDocument
            {
                Id = documentId,
                CommandId = commandId.Value
            },
            cancellationToken);

        return true;
    }

    public async Task MarkCompletedAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDocument.GetDocumentId(commandId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDocument>(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Lookup execution receipt '{documentId}' was not found.");

        existing.Completed = true;
    }
}
