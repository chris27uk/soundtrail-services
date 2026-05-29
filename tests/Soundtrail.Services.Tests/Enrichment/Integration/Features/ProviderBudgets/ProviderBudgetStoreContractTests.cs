using FluentAssertions;
using Soundtrail.Services.EnrichmentWorker.Budgets;
using Soundtrail.Services.EnrichmentWorker.Infrastructure.AzureTable;
using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.EnrichmentWorker.Tests.Integration.Features.ProviderBudgets;

public sealed class ProviderBudgetStoreContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { BudgetStoreMode.Fake };
        yield return new object[] { BudgetStoreMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Provider_Budget_With_A_One_Call_Limit_When_Budget_Is_Consumed_Twice_Then_Only_The_First_Call_Is_Allowed(BudgetStoreMode mode)
    {
        var env = ProviderBudgetStoreTestEnvironment.Create(mode);
        var policy = new ProviderRateLimitPolicy(
            ProviderName.AppleMusic,
            Enabled: true,
            MaxPerMinute: 1,
            MaxPerHour: 10,
            MaxPerDay: 100,
            MaxConcurrent: 1,
            MinimumPriorityScore: 100,
            CircuitBreakerFailureThreshold: 5,
            CircuitBreakerOpenMinutes: 30,
            NegativeCacheDays: 30,
            CostPenalty: 50);

        var first = await env.Budgets.TryConsumeAsync(ProviderName.AppleMusic, policy, CancellationToken.None);
        var second = await env.Budgets.TryConsumeAsync(ProviderName.AppleMusic, policy, CancellationToken.None);

        first.Allowed.Should().BeTrue();
        second.Allowed.Should().BeFalse();
    }
}

public enum BudgetStoreMode
{
    Fake = 0,
    AzureTable = 1
}

internal sealed class ProviderBudgetStoreTestEnvironment
{
    private ProviderBudgetStoreTestEnvironment(IProviderBudgetPort budgets)
    {
        Budgets = budgets;
    }

    public IProviderBudgetPort Budgets { get; }

    public static ProviderBudgetStoreTestEnvironment Create(BudgetStoreMode mode) =>
        mode switch
        {
            BudgetStoreMode.Fake => new(new FakeProviderBudgetPort()),
            BudgetStoreMode.AzureTable => new(new AzureTableProviderBudgetStore(new FixedBudgetClock())),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}

internal sealed class FakeProviderBudgetPort : IProviderBudgetPort
{
    private readonly Dictionary<string, int> usageByMinute = [];
    private readonly FixedBudgetClock clock = new();

    public Task<bool> IsAvailableAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var key = $"{provider}:{now:yyyyMMddHHmm}";
        usageByMinute.TryGetValue(key, out var current);
        return Task.FromResult(current < policy.MaxPerMinute);
    }

    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var key = $"{provider}:{now:yyyyMMddHHmm}";
        usageByMinute.TryGetValue(key, out var current);
        var next = current + 1;
        usageByMinute[key] = next;

        return Task.FromResult(new ProviderBudgetDecision(
            next <= policy.MaxPerMinute,
            next,
            policy.MaxPerMinute,
            next,
            policy.MaxPerHour,
            next,
            policy.MaxPerDay));
    }
}

internal sealed class FixedBudgetClock : IClockPort
{
    public DateTimeOffset UtcNow => new(2026, 5, 29, 12, 0, 0, TimeSpan.Zero);
}
