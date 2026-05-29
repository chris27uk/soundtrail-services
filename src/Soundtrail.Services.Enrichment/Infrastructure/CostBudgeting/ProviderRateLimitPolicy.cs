using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Budgets;

public sealed record ProviderRateLimitPolicy(
    ProviderName Provider,
    bool Enabled,
    int MaxPerMinute,
    int MaxPerHour,
    int MaxPerDay,
    int MaxConcurrent,
    int MinimumPriorityScore,
    int CircuitBreakerFailureThreshold,
    int CircuitBreakerOpenMinutes,
    int NegativeCacheDays,
    int CostPenalty)
{
    public static ProviderRateLimitPolicy For(ProviderName provider) =>
        provider switch
        {
            ProviderName.MusicBrainz => new(provider, true, 20, 500, 5000, 2, 10, 10, 15, 7, 10),
            ProviderName.AppleMusic => new(provider, true, 5, 100, 500, 1, 100, 5, 30, 30, 50),
            ProviderName.ITunesSearch => new(provider, true, 10, 100, 300, 1, 150, 5, 30, 30, 75),
            _ => new(provider, true, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, 0, int.MaxValue, 0, 0, 0)
        };
}
