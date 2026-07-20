using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;

public interface ILookupExecutionReceiptStore
{
    Task<bool> TryBeginAsync(
        MessageId messageId,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        MessageId messageId,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        MessageId messageId,
        CancellationToken cancellationToken);
}
