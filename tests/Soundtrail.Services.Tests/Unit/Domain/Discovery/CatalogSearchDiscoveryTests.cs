using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Domain.Discovery;

public sealed class CatalogSearchDiscoveryTests
{
    [Fact]
    public async Task Given_An_Empty_Discovery_When_Requesting_Then_DiscoveryRequested_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var requested = discovery.Request(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);
        var @event = repository.GetStoredEvents(criteria).Should().ContainSingle().Which.Should().BeOfType<DiscoveryRequested>().Subject;

        requested.Should().BeTrue();
        @event.Criteria.Should().Be(criteria);
        @event.Query.Should().Be(NormalizedSearchQuery.FromText("rare unknown song"));
    }

    [Fact]
    public async Task Given_A_Discovery_That_Has_Already_Been_Requested_When_Requesting_Again_Then_No_New_Event_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Request(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);
        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var requested = discovery.Request(Request(criteria));

        requested.Should().BeFalse();
        repository.GetStoredEvents(criteria).Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Requested_Discovery_When_Planning_Then_DiscoveryPlanned_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Request(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);
        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var planned = discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);
        var @event = repository.GetStoredEvents(criteria).Last().Should().BeOfType<DiscoveryPlanned>().Subject;

        planned.Should().BeTrue();
        @event.Priority.Should().Be(LookupPriorityBand.High);
        @event.EstimatedRetryAfterSeconds.Should().Be(30);
        @event.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_A_Requested_Discovery_When_Deferring_Then_DiscoveryDeferred_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Request(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);
        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var deferred = discovery.Defer(60, Clock.AddSeconds(60), "Planner deferred lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);
        var @event = repository.GetStoredEvents(criteria).Last().Should().BeOfType<DiscoveryDeferred>().Subject;

        deferred.Should().BeTrue();
        @event.EstimatedRetryAfterSeconds.Should().Be(60);
        @event.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_A_Discovery_When_Rejecting_Then_DiscoveryRejected_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var rejected = discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);
        var @event = repository.GetStoredEvents(criteria).Should().ContainSingle().Which.Should().BeOfType<DiscoveryRejected>().Subject;

        rejected.Should().BeTrue();
        @event.WillBeLookedUp.Should().BeFalse();
        @event.Reason.Should().Be("Planner rejected lookup");
    }

    [Fact]
    public async Task Given_A_Discovery_When_Failing_Then_DiscoveryFailed_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var failed = discovery.Fail("Lookup failed", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);
        var @event = repository.GetStoredEvents(criteria).Should().ContainSingle().Which.Should().BeOfType<DiscoveryFailed>().Subject;

        failed.Should().BeTrue();
        @event.WillBeLookedUp.Should().BeFalse();
        @event.Reason.Should().Be("Lookup failed");
    }

    [Fact]
    public async Task Given_A_Rejected_Discovery_When_Planning_Then_The_Transition_Is_Rejected()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);
        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);

        var act = () => discovery.Plan(LookupPriorityBand.Low, 30, null, "Planner queued lookup", Clock);

        act.Should().Throw<InvalidOperationException>();
    }

    private static CatalogSearchAttempt Request(CatalogSearchCriteria criteria) =>
        new(
            criteria,
            NormalizedSearchQuery.FromText("rare unknown song"),
            1,
            10,
            Clock,
            CorrelationId.From("corr-1"));

    private static readonly DateTimeOffset Clock = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
}
