using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

internal sealed class RavenLookupExecutionReceiptStore(
    IAsyncDocumentSession session) : ILookupExecutionReceiptStore
{
    public async Task<bool> TryBeginAsync(
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDto.GetDocumentId(messageId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDto>(documentId, cancellationToken);
        if (existing is not null)
        {
            return false;
        }

        await session.StoreAsync(
            new RavenLookupExecutionReceiptDto
            {
                Id = documentId,
                CommandId = messageId.Value
            },
            cancellationToken);

        return true;
    }

    public async Task MarkCompletedAsync(
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDto.GetDocumentId(messageId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDto>(documentId, cancellationToken)
            ?? throw new InvalidOperationException($"Lookup execution receipt '{documentId}' was not found.");

        existing.Completed = true;
    }

    public async Task ReleaseAsync(
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        var documentId = RavenLookupExecutionReceiptDto.GetDocumentId(messageId.Value);
        var existing = await session.LoadAsync<RavenLookupExecutionReceiptDto>(documentId, cancellationToken);
        if (existing is null || existing.Completed)
        {
            return;
        }

        session.Delete(existing);
    }
}
