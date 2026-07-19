using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;

public interface IActiveLookupWorkStore
{
    Task<bool> TryAcquireAsync(
        CommandId commandId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken);
}
