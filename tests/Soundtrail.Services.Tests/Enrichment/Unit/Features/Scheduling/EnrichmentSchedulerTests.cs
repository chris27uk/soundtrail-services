using FluentAssertions;
using Soundtrail.Services.Enrichment.Budgets;
using Soundtrail.Services.Enrichment.Configuration;
using Soundtrail.Services.Enrichment.Jobs;
using Soundtrail.Services.Enrichment.Models;
using Soundtrail.Services.Enrichment.Ports;
using Soundtrail.Services.Enrichment.Scheduling;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Worker.Tests.Unit.Features.Scheduling;

public sealed class EnrichmentSchedulerTests
{
    [Fact]
    public async Task Given_Higher_And_Lower_Demand_When_The_Scheduler_Runs_Then_It_Enqueues_The_Higher_Priority_Demand_First()
    {
        var env = EnrichmentSchedulerTestEnvironment.WithCompetingDemand();

        var jobs = await env.CreateScheduler().RunAsync(CancellationToken.None);

        jobs.Should().HaveCount(2);
        jobs[0].QueryId.Should().Be(env.PopularDemand.QueryId);
        jobs[1].QueryId.Should().Be(env.ObscureDemand.QueryId);
    }

    [Fact]
    public async Task Given_High_Priority_Isrc_Demand_When_The_Scheduler_Runs_Then_It_Prefers_Cheaper_Stages_Before_Apple()
    {
        var env = EnrichmentSchedulerTestEnvironment.WithPopularIsrcDemand();

        var jobs = await env.CreateScheduler().RunAsync(CancellationToken.None);

        jobs.Should().ContainSingle();
        jobs[0].Stage.Should().Be(EnrichmentStage.LocalMapping);
    }

    [Fact]
    public async Task Given_A_Disabled_Provider_When_The_Scheduler_Runs_Then_It_Skips_That_Job()
    {
        var env = EnrichmentSchedulerTestEnvironment.WithPopularIsrcDemand();
        env.WorkerOptions = new EnrichmentWorkerOptions
        {
            MusicBrainz = ProviderRateLimitPolicy.For(ProviderName.MusicBrainz) with { Enabled = false }
        };
        env.PopularDemandAttempted(EnrichmentStage.LocalMapping, EnrichmentStage.LocalMusicBrainzDataset);

        var jobs = await env.CreateScheduler().RunAsync(CancellationToken.None);

        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_An_Open_Circuit_When_The_Scheduler_Runs_Then_It_Skips_That_Provider()
    {
        var env = EnrichmentSchedulerTestEnvironment.WithPopularIsrcDemand();
        env.PopularDemandAttempted(EnrichmentStage.LocalMapping, EnrichmentStage.LocalMusicBrainzDataset);
        env.CircuitState.Open(ProviderName.MusicBrainz);

        var jobs = await env.CreateScheduler().RunAsync(CancellationToken.None);

        jobs.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Provider_With_No_Available_Budget_When_The_Scheduler_Runs_Then_It_Prefers_A_Candidate_With_Available_Budget()
    {
        var env = EnrichmentSchedulerTestEnvironment.WithCompetingDemand();
        env.PopularDemandAttempted(EnrichmentStage.LocalMapping, EnrichmentStage.LocalMusicBrainzDataset);
        env.ProviderBudgets.Deny(ProviderName.MusicBrainz);

        var jobs = await env.CreateScheduler().RunAsync(CancellationToken.None);

        jobs.Should().ContainSingle();
        jobs[0].QueryId.Should().Be(env.ObscureDemand.QueryId);
    }
}

internal sealed class EnrichmentSchedulerTestEnvironment
{
    private readonly FakeDemandStore demandStore;
    private readonly FakeEnrichmentQueue queue;
    private readonly FakeProviderBudgetStore providerBudgets;
    private readonly FakeProviderCircuitStateStore circuitState;
    private readonly FakeProviderConcurrencyPort providerConcurrency;
    private readonly FakeClock clock;

    private EnrichmentSchedulerTestEnvironment(
        FakeDemandStore demandStore,
        FakeEnrichmentQueue queue,
        FakeProviderBudgetStore providerBudgets,
        FakeProviderCircuitStateStore circuitState,
        FakeProviderConcurrencyPort providerConcurrency,
        FakeClock clock)
    {
        this.demandStore = demandStore;
        this.queue = queue;
        this.providerBudgets = providerBudgets;
        this.circuitState = circuitState;
        this.providerConcurrency = providerConcurrency;
        this.clock = clock;
    }

    public EnrichmentWorkerOptions WorkerOptions { get; set; } = new();

    public ResolutionDemand PopularDemand => demandStore.Demand[0];

    public ResolutionDemand ObscureDemand => demandStore.Demand[^1];

    public FakeProviderBudgetStore ProviderBudgets => providerBudgets;

    public FakeProviderCircuitStateStore CircuitState => circuitState;

    public EnrichmentScheduler CreateScheduler() =>
        new(
            demandStore,
            queue,
            providerConcurrency,
            providerBudgets,
            circuitState,
            new EnrichmentCandidateSelector(
                new EnrichmentPriorityCalculator(WorkerOptions),
                new NextStageDecider()),
            WorkerOptions,
            clock);

    public void PopularDemandAttempted(params EnrichmentStage[] stages)
    {
        demandStore.Replace(PopularDemand with { AttemptedStages = stages });
    }

    public static EnrichmentSchedulerTestEnvironment WithPopularIsrcDemand()
    {
        return new EnrichmentSchedulerTestEnvironment(
            new FakeDemandStore(KnownDemand.PopularWithIsrc()),
            new FakeEnrichmentQueue(),
            new FakeProviderBudgetStore(),
            new FakeProviderCircuitStateStore(),
            new FakeProviderConcurrencyPort(),
            FakeClock.Fixed());
    }

    public static EnrichmentSchedulerTestEnvironment WithCompetingDemand()
    {
        return new EnrichmentSchedulerTestEnvironment(
            new FakeDemandStore(KnownDemand.PopularWithIsrc(), KnownDemand.ObscureWithArtistAndTitle()),
            new FakeEnrichmentQueue(),
            new FakeProviderBudgetStore(),
            new FakeProviderCircuitStateStore(),
            new FakeProviderConcurrencyPort(),
            FakeClock.Fixed());
    }
}

internal sealed class FakeDemandStore(params ResolutionDemand[] demand) : IDemandStorePort
{
    public List<ResolutionDemand> Demand { get; } = demand.ToList();

    public Task<IReadOnlyList<ResolutionDemand>> GetUnresolvedAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ResolutionDemand>>(Demand);

    public Task<ResolutionDemand?> GetAsync(QueryId queryId, CancellationToken cancellationToken) =>
        Task.FromResult(Demand.SingleOrDefault(candidate => candidate.QueryId == queryId));

    public Task UpsertAsync(ResolutionDemand demand, CancellationToken cancellationToken)
    {
        Replace(demand);
        return Task.CompletedTask;
    }

    public Task MarkResolvedAsync(QueryId queryId, CancellationToken cancellationToken) => Task.CompletedTask;

    public void Replace(ResolutionDemand demand)
    {
        var index = Demand.FindIndex(candidate => candidate.QueryId == demand.QueryId);
        if (index >= 0)
        {
            Demand[index] = demand;
        }
        else
        {
            Demand.Add(demand);
        }
    }
}

internal sealed class FakeEnrichmentQueue : IEnrichmentQueuePort
{
    public List<EnrichmentJob> Jobs { get; } = [];

    public Task EnqueueAsync(EnrichmentJob job, CancellationToken cancellationToken)
    {
        Jobs.Add(job);
        return Task.CompletedTask;
    }
}

internal sealed class FakeProviderBudgetStore : IProviderBudgetPort
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

internal sealed class FakeProviderCircuitStateStore : IProviderCircuitStatePort
{
    private readonly Dictionary<ProviderName, CircuitState> states = [];

    public Task<ProviderCircuitState> GetAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(new ProviderCircuitState(
            provider,
            states.TryGetValue(provider, out var state) ? state : CircuitState.Closed,
            0,
            null,
            null,
            null,
            null));

    public Task UpsertAsync(ProviderCircuitState state, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public void Open(ProviderName provider) => states[provider] = CircuitState.Open;
}

internal sealed class FakeProviderConcurrencyPort : IProviderConcurrencyPort
{
    public Task<bool> IsAvailableAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<ConcurrencyLease?> TryAcquireAsync(ProviderName provider, CancellationToken cancellationToken) =>
        Task.FromResult<ConcurrencyLease?>(new ConcurrencyLease(Guid.NewGuid().ToString("N"), provider, "test", DateTimeOffset.UtcNow.AddMinutes(1)));

    public Task ReleaseAsync(ConcurrencyLease lease, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakeClock : IClockPort
{
    private FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }

    public static FakeClock Fixed() => new(new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero));
}

internal static class KnownDemand
{
    public static ResolutionDemand PopularWithIsrc() =>
        new(
            QueryId.New(),
            NormalizedSearchQuery.FromText("mr brightside"),
            DemandCount: 50,
            DistinctInstallCount: 15,
            DistinctIpHashCount: 12,
            HighestTrustLevelSeen: 2,
            RiskScore: 0,
            BestKnownTitle: TrackTitle.From("Mr. Brightside"),
            BestKnownArtist: ArtistName.From("The Killers"),
            BestKnownIsrc: Isrc.From("USIR20400274"),
            BestKnownMbid: null,
            Status: ResolutionDemandStatus.Unresolved,
            FirstSeenAt: FakeClock.Fixed().UtcNow.AddHours(-2),
            LastSeenAt: FakeClock.Fixed().UtcNow.AddMinutes(-5),
            NextEligibleAt: null);

    public static ResolutionDemand ObscureWithArtistAndTitle() =>
        new(
            QueryId.New(),
            NormalizedSearchQuery.FromText("obscure demo"),
            DemandCount: 1,
            DistinctInstallCount: 1,
            DistinctIpHashCount: 1,
            HighestTrustLevelSeen: 0,
            RiskScore: 0,
            BestKnownTitle: TrackTitle.From("Obscure Demo"),
            BestKnownArtist: ArtistName.From("Unknown Artist"),
            BestKnownIsrc: null,
            BestKnownMbid: null,
            Status: ResolutionDemandStatus.Unresolved,
            FirstSeenAt: FakeClock.Fixed().UtcNow.AddHours(-1),
            LastSeenAt: FakeClock.Fixed().UtcNow.AddMinutes(-1),
            NextEligibleAt: null);
}
