using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Idempotency;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

internal sealed class ActiveLookupWorkStoreFake : IActiveLookupWorkStore
{
    private readonly Dictionary<string, ActiveLookupWork> locksByCommandId = [];

    public IReadOnlyCollection<ActiveLookupWork> Locks => this.locksByCommandId.Values.ToArray();

    public Task<bool> TryAcquireAsync(
        CommandId commandId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        if (this.locksByCommandId.TryGetValue(commandId.Value, out var existing) &&
            existing.ExpiresAt > expiresAt.AddYears(-100))
        {
            return Task.FromResult(false);
        }

        this.locksByCommandId[commandId.Value] = new ActiveLookupWork(commandId, expiresAt);
        return Task.FromResult(true);
    }

    public Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (this.locksByCommandId.TryGetValue(commandId.Value, out var existing) &&
            existing.CommandId == commandId)
        {
            this.locksByCommandId.Remove(commandId.Value);
        }

        return Task.CompletedTask;
    }
}
