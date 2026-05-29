using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using System.Collections.Concurrent;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CostBudgeting;

public sealed class AzureTableProviderConcurrencyStore : IProviderConcurrencyPort
{
    private readonly ConcurrentDictionary<string, ConcurrencyLease> activeLeases = new();
    private readonly object sync = new();
    private readonly IClockPort clock;
    private readonly Func<ProviderName, int> maxConcurrentResolver;

    public AzureTableProviderConcurrencyStore()
        : this(
            new UtcClock(),
            static provider => provider switch
            {
                ProviderName.MusicBrainz => 3,
                ProviderName.AppleMusic => 1,
                ProviderName.ITunesSearch => 1,
                _ => int.MaxValue
            })
    {
    }

    public AzureTableProviderConcurrencyStore(
        IClockPort clock,
        Func<ProviderName, int> maxConcurrentResolver)
    {
        this.clock = clock;
        this.maxConcurrentResolver = maxConcurrentResolver;
    }

    public Task<ConcurrencyLease?> TryAcquireAsync(
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        lock (sync)
        {
            RemoveExpiredLeases();

            var activeCount = activeLeases.Values.Count(lease => lease.Provider == provider);
            if (activeCount >= maxConcurrentResolver(provider))
            {
                return Task.FromResult<ConcurrencyLease?>(null);
            }

            var lease = new ConcurrencyLease(
                LeaseId: Guid.NewGuid().ToString("N"),
                provider,
                LeaseOwner: Environment.MachineName,
                ExpiresAt: clock.UtcNow.AddMinutes(1));

            activeLeases[ProviderScopedLeaseKey(provider, lease.LeaseId)] = lease;
            return Task.FromResult<ConcurrencyLease?>(lease);
        }
    }

    public Task<bool> IsAvailableAsync(
        ProviderName provider,
        CancellationToken cancellationToken)
    {
        lock (sync)
        {
            RemoveExpiredLeases();
            var activeCount = activeLeases.Values.Count(lease => lease.Provider == provider);
            return Task.FromResult(activeCount < maxConcurrentResolver(provider));
        }
    }

    public Task ReleaseAsync(
        ConcurrencyLease lease,
        CancellationToken cancellationToken)
    {
        lock (sync)
        {
            activeLeases.TryRemove(ProviderScopedLeaseKey(lease.Provider, lease.LeaseId), out _);
            return Task.CompletedTask;
        }
    }

    private void RemoveExpiredLeases()
    {
        foreach (var pair in activeLeases.ToArray())
        {
            if (pair.Value.ExpiresAt <= clock.UtcNow)
            {
                activeLeases.TryRemove(pair.Key, out _);
            }
        }
    }

    private static string ProviderScopedLeaseKey(ProviderName provider, string leaseId) => $"{provider}:{leaseId}";

    private sealed class UtcClock : IClockPort
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
