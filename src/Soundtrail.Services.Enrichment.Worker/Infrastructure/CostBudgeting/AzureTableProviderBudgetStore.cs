using Soundtrail.Services.Enrichment.Infrastructure.CostBudgeting;
using System.Collections.Concurrent;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CostBudgeting;

public sealed class AzureTableProviderBudgetStore : IProviderBudgetPort
{
    private readonly ConcurrentDictionary<string, int> usage = new();
    private readonly object sync = new();
    private readonly IClockPort clock;

    public AzureTableProviderBudgetStore()
        : this(new UtcClock())
    {
    }

    public AzureTableProviderBudgetStore(IClockPort clock)
    {
        this.clock = clock;
    }

    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!policy.Enabled)
        {
            return Task.FromResult(new ProviderBudgetDecision(false, 0, policy.MaxPerMinute, 0, policy.MaxPerHour, 0, policy.MaxPerDay));
        }

        lock (sync)
        {
            var now = clock.UtcNow;
            var minuteKey = $"{provider}:minute:{now:yyyyMMddHHmm}";
            var hourKey = $"{provider}:hour:{now:yyyyMMddHH}";
            var dayKey = $"{provider}:day:{now:yyyyMMdd}";

            usage.TryGetValue(minuteKey, out var minuteUsed);
            usage.TryGetValue(hourKey, out var hourUsed);
            usage.TryGetValue(dayKey, out var dayUsed);

            var allowed =
                minuteUsed < policy.MaxPerMinute &&
                hourUsed < policy.MaxPerHour &&
                dayUsed < policy.MaxPerDay;

            if (allowed)
            {
                minuteUsed = usage.AddOrUpdate(minuteKey, 1, (_, current) => current + 1);
                hourUsed = usage.AddOrUpdate(hourKey, 1, (_, current) => current + 1);
                dayUsed = usage.AddOrUpdate(dayKey, 1, (_, current) => current + 1);
            }

            return Task.FromResult(new ProviderBudgetDecision(
                allowed,
                minuteUsed,
                policy.MaxPerMinute,
                hourUsed,
                policy.MaxPerHour,
                dayUsed,
                policy.MaxPerDay));
        }
    }

    public Task<bool> IsAvailableAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        if (!policy.Enabled)
        {
            return Task.FromResult(false);
        }

        lock (sync)
        {
            var now = clock.UtcNow;
            var minuteKey = $"{provider}:minute:{now:yyyyMMddHHmm}";
            var hourKey = $"{provider}:hour:{now:yyyyMMddHH}";
            var dayKey = $"{provider}:day:{now:yyyyMMdd}";

            usage.TryGetValue(minuteKey, out var minuteUsed);
            usage.TryGetValue(hourKey, out var hourUsed);
            usage.TryGetValue(dayKey, out var dayUsed);

            return Task.FromResult(
                minuteUsed < policy.MaxPerMinute &&
                hourUsed < policy.MaxPerHour &&
                dayUsed < policy.MaxPerDay);
        }
    }

    private sealed class UtcClock : IClockPort
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
