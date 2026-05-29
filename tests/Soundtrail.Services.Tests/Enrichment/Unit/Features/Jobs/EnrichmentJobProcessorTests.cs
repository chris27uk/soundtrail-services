using FluentAssertions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.EnrichmentWorker.Budgets;
using Soundtrail.Services.EnrichmentWorker.Configuration;
using Soundtrail.Services.EnrichmentWorker.Jobs;
using Soundtrail.Services.EnrichmentWorker.Models;
using Soundtrail.Services.EnrichmentWorker.Ports;
using Soundtrail.Services.EnrichmentWorker.Providers;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.EnrichmentWorker.Tests.Unit.Features.Jobs;

public sealed class EnrichmentJobProcessorTests
{
    [Fact]
    public async Task Given_No_Concurrency_Lease_When_A_Third_Party_Job_Is_Processed_Then_The_Provider_Is_Not_Called()
    {
        var env = EnrichmentJobProcessorTestEnvironment.WithThirdPartyJob();
        env.Concurrency.Deny();

        var result = await env.CreateProcessor().ProcessAsync(env.Job, CancellationToken.None);

        result.Outcome.Should().Be(EnrichmentOutcome.RetryLater);
        env.Provider.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_An_Exhausted_Budget_When_A_Third_Party_Job_Is_Processed_Then_The_Provider_Is_Not_Called()
    {
        var env = EnrichmentJobProcessorTestEnvironment.WithThirdPartyJob();
        env.Budgets.Deny(ProviderName.MusicBrainz);

        var result = await env.CreateProcessor().ProcessAsync(env.Job, CancellationToken.None);

        result.Outcome.Should().Be(EnrichmentOutcome.RetryLater);
        env.Provider.CallCount.Should().Be(0);
        env.Concurrency.ReleaseCount.Should().Be(1);
    }
}

internal sealed class EnrichmentJobProcessorTestEnvironment
{
    private readonly FakeJobDemandStore demandStore;
    private readonly FakeJobProviderCircuitStateStore circuitState;
    private readonly FakeJobProviderConcurrencyPort concurrency;
    private readonly FakeJobProviderBudgetStore budgets;
    private readonly FakeEnrichmentAttemptStore attemptStore;
    private readonly FakeMappingStore mappingStore;
    private readonly FakeQueryCache queryCache;
    private readonly FakeSearchIndex searchIndex;
    private readonly SpyProvider provider;
    private readonly FakeJobClock clock;

    private EnrichmentJobProcessorTestEnvironment(
        FakeJobDemandStore demandStore,
        FakeJobProviderCircuitStateStore circuitState,
        FakeJobProviderConcurrencyPort concurrency,
        FakeJobProviderBudgetStore budgets,
        FakeEnrichmentAttemptStore attemptStore,
        FakeMappingStore mappingStore,
        FakeQueryCache queryCache,
        FakeSearchIndex searchIndex,
        SpyProvider provider,
        FakeJobClock clock)
    {
        this.demandStore = demandStore;
        this.circuitState = circuitState;
        this.concurrency = concurrency;
        this.budgets = budgets;
        this.attemptStore = attemptStore;
        this.mappingStore = mappingStore;
        this.queryCache = queryCache;
        this.searchIndex = searchIndex;
        this.provider = provider;
        this.clock = clock;
    }

    public EnrichmentJob Job => new(
        "job-1",
        demandStore.Demand.QueryId,
        demandStore.Demand.NormalizedQuery,
        EnrichmentStage.MusicBrainzApi,
        ProviderName.MusicBrainz,
        100,
        0,
        clock.UtcNow,
        clock.UtcNow,
        "correlation");

    public FakeJobProviderConcurrencyPort Concurrency => concurrency;

    public FakeJobProviderBudgetStore Budgets => budgets;

    public SpyProvider Provider => provider;

    public EnrichmentJobProcessor CreateProcessor() =>
        new(
            demandStore,
            circuitState,
            concurrency,
            budgets,
            attemptStore,
            mappingStore,
            queryCache,
            searchIndex,
            new[] { provider },
            Options.Create(new EnrichmentWorkerOptions()),
            clock);

    public static EnrichmentJobProcessorTestEnvironment WithThirdPartyJob()
    {
        var demand = new ResolutionDemand(
            QueryId.New(),
            NormalizedSearchQuery.FromText("mr brightside"),
            10,
            5,
            5,
            1,
            0,
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            null,
            ResolutionDemandStatus.Unresolved,
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow,
            null);

        return new EnrichmentJobProcessorTestEnvironment(
            new FakeJobDemandStore(demand),
            new FakeJobProviderCircuitStateStore(),
            new FakeJobProviderConcurrencyPort(),
            new FakeJobProviderBudgetStore(),
            new FakeEnrichmentAttemptStore(),
            new FakeMappingStore(),
            new FakeQueryCache(),
            new FakeSearchIndex(),
            new SpyProvider(),
            new FakeJobClock());
    }
}

internal sealed class FakeJobDemandStore(ResolutionDemand demand) : IDemandStorePort
{
    public ResolutionDemand Demand { get; } = demand;

    public Task<IReadOnlyList<ResolutionDemand>> GetUnresolvedAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ResolutionDemand>>([Demand]);

    public Task<ResolutionDemand?> GetAsync(QueryId queryId, CancellationToken cancellationToken) =>
        Task.FromResult<ResolutionDemand?>(Demand);

    public Task UpsertAsync(ResolutionDemand demand, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task MarkResolvedAsync(QueryId queryId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeJobProviderCircuitStateStore : IProviderCircuitStatePort
{
    public Task<ProviderCircuitState> GetAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderCircuitState(provider, CircuitState.Closed, 0, null, null, null, null));

    public Task UpsertAsync(ProviderCircuitState state, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeJobProviderConcurrencyPort : IProviderConcurrencyPort
{
    private bool deny;

    public int ReleaseCount { get; private set; }

    public Task<bool> IsAvailableAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(!deny);

    public Task<ConcurrencyLease?> TryAcquireAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(
            deny
                ? null
                : new ConcurrencyLease("lease-1", provider, "test-worker", DateTimeOffset.UtcNow.AddMinutes(1)));

    public Task ReleaseAsync(ConcurrencyLease lease, CancellationToken cancellationToken)
    {
        ReleaseCount++;
        return Task.CompletedTask;
    }

    public void Deny() => deny = true;
}

internal sealed class FakeJobProviderBudgetStore : IProviderBudgetPort
{
    private readonly HashSet<ProviderName> deniedProviders = [];

    public Task<bool> IsAvailableAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken) =>
        Task.FromResult(!deniedProviders.Contains(provider));

    public Task<ProviderBudgetDecision> TryConsumeAsync(
        ProviderName provider,
        ProviderRateLimitPolicy policy,
        CancellationToken cancellationToken) =>
        Task.FromResult(
            deniedProviders.Contains(provider)
                ? new ProviderBudgetDecision(false, 0, policy.MaxPerMinute, 0, policy.MaxPerHour, 0, policy.MaxPerDay)
                : new ProviderBudgetDecision(true, 1, policy.MaxPerMinute, 1, policy.MaxPerHour, 1, policy.MaxPerDay));

    public void Deny(ProviderName provider) => deniedProviders.Add(provider);
}

internal sealed class FakeEnrichmentAttemptStore : IEnrichmentAttemptStorePort
{
    public Task RecordAsync(EnrichmentAttempt attempt, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeMappingStore : IMappingStorePort
{
    public Task<TrackMapping?> FindAsync(ResolutionDemand demand, CancellationToken cancellationToken) =>
        Task.FromResult<TrackMapping?>(null);

    public Task UpsertAsync(TrackMapping mapping, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeQueryCache : Soundtrail.Services.EnrichmentWorker.Ports.IQueryCachePort
{
    public Task RefreshAsync(ResolutionDemand demand, TrackMapping mapping, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeSearchIndex : ISearchIndexPort
{
    public Task UpsertAsync(TrackMapping mapping, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class SpyProvider : IEnrichmentProvider
{
    public int CallCount { get; private set; }

    public EnrichmentStage Stage => EnrichmentStage.MusicBrainzApi;

    public ProviderName Provider => ProviderName.MusicBrainz;

    public Task<EnrichmentJobResult> EnrichAsync(ResolutionDemand demand, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(new EnrichmentJobResult(EnrichmentOutcome.NotFound));
    }
}

internal sealed class FakeJobClock : IClockPort
{
    public DateTimeOffset UtcNow => new(2026, 5, 29, 12, 0, 0, TimeSpan.Zero);
}
