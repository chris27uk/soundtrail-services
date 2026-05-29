using FluentAssertions;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CostBudgeting;
using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker.Tests.Integration.Features.ProviderBudgets;

public sealed class ProviderConcurrencyStoreContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { ConcurrencyStoreMode.Fake };
        yield return new object[] { ConcurrencyStoreMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Single_Slot_Provider_When_Two_Leases_Are_Acquired_Then_Only_The_First_Is_Granted(ConcurrencyStoreMode mode)
    {
        var env = ProviderConcurrencyStoreTestEnvironment.Create(mode);

        var first = await env.Concurrency.TryAcquireAsync(ProviderName.AppleMusic, CancellationToken.None);
        var second = await env.Concurrency.TryAcquireAsync(ProviderName.AppleMusic, CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_An_Expired_Lease_When_A_Lease_Is_Acquired_Again_Then_It_Can_Be_Reacquired(ConcurrencyStoreMode mode)
    {
        var env = ProviderConcurrencyStoreTestEnvironment.Create(mode);

        var first = await env.Concurrency.TryAcquireAsync(ProviderName.AppleMusic, CancellationToken.None);
        env.Clock.AdvanceBy(TimeSpan.FromMinutes(2));
        var second = await env.Concurrency.TryAcquireAsync(ProviderName.AppleMusic, CancellationToken.None);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
    }
}

public enum ConcurrencyStoreMode
{
    Fake = 0,
    AzureTable = 1
}

internal sealed class ProviderConcurrencyStoreTestEnvironment
{
    private ProviderConcurrencyStoreTestEnvironment(IProviderConcurrencyPort concurrency, MutableClock clock)
    {
        Concurrency = concurrency;
        Clock = clock;
    }

    public IProviderConcurrencyPort Concurrency { get; }

    public MutableClock Clock { get; }

    public static ProviderConcurrencyStoreTestEnvironment Create(ConcurrencyStoreMode mode)
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));

        return mode switch
        {
            ConcurrencyStoreMode.Fake => new(new FakeProviderConcurrencyStore(clock), clock),
            ConcurrencyStoreMode.AzureTable => new(
                new AzureTableProviderConcurrencyStore(clock, static provider => provider == ProviderName.AppleMusic ? 1 : 3),
                clock),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}

internal sealed class FakeProviderConcurrencyStore(MutableClock clock) : IProviderConcurrencyPort
{
    private readonly List<ConcurrencyLease> leases = [];

    public Task<bool> IsAvailableAsync(ProviderName provider, CancellationToken cancellationToken)
    {
        leases.RemoveAll(lease => lease.ExpiresAt <= clock.UtcNow);
        return Task.FromResult(!leases.Any(lease => lease.Provider == provider));
    }

    public Task<ConcurrencyLease?> TryAcquireAsync(ProviderName provider, CancellationToken cancellationToken)
    {
        leases.RemoveAll(lease => lease.ExpiresAt <= clock.UtcNow);

        if (leases.Any(lease => lease.Provider == provider))
        {
            return Task.FromResult<ConcurrencyLease?>(null);
        }

        var lease = new ConcurrencyLease(Guid.NewGuid().ToString("N"), provider, "fake", clock.UtcNow.AddMinutes(1));
        leases.Add(lease);
        return Task.FromResult<ConcurrencyLease?>(lease);
    }

    public Task ReleaseAsync(ConcurrencyLease lease, CancellationToken cancellationToken)
    {
        leases.RemoveAll(candidate => candidate.LeaseId == lease.LeaseId);
        return Task.CompletedTask;
    }
}

internal sealed class MutableClock(DateTimeOffset utcNow) : IClockPort
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public void AdvanceBy(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
