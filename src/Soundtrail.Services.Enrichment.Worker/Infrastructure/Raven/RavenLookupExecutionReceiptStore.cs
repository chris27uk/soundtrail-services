using Raven.Client.Documents.Session;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

internal sealed class RavenLookupExecutionReceiptStore(
    IAsyncDocumentSession session) : ILookupExecutionReceiptStore
{
    public async Task<bool> TryBeginAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDto.GetDocumentId(commandId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDto>(documentId, cancellationToken);
        if (existing is not null)
        {
            return false;
        }

        await session.StoreAsync(
            new RavenLookupExecutionReceiptDto
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
        var documentId = RavenLookupExecutionReceiptDto.GetDocumentId(commandId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDto>(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Lookup execution receipt '{documentId}' was not found.");

        existing.Completed = true;
    }
}
