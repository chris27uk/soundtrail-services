using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.Idempotency;

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
