using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;

public interface IActiveLookupWorkStore
{
    Task<bool> TryAcquireAsync(
        MessageId messageId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        MessageId messageId,
        CancellationToken cancellationToken);
}
