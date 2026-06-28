using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Domain.Discovery;

public sealed class CatalogSearchDiscoveryTests
{
    [Fact]
    public async Task Given_An_Empty_Discovery_When_Requesting_Then_DiscoveryRequested_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;

        var requested = discovery.SearchRequested(Request(searchTerm));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        var @event = repository.GetStoredEvents(searchTerm).Should().ContainSingle().Which.Should().BeOfType<DiscoveryRequested>().Subject;

        requested.Should().BeTrue();
        @event.SearchCriteria.Should().Be(searchTerm);
        @event.TrustLevel.Should().Be(1);
        @event.RiskScore.Should().Be(10);
    }

    [Fact]
    public async Task Given_A_Discovery_That_Has_Already_Been_Requested_When_Requesting_Again_Then_No_New_Event_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(searchTerm));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var requested = discovery.SearchRequested(Request(searchTerm));

        requested.Should().BeFalse();
        repository.GetStoredEvents(searchTerm).Should().ContainSingle();
    }

    [Fact]
    public async Task Given_A_Requested_Discovery_When_Planning_Then_DiscoveryPlanned_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(searchTerm));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var planned = discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        var @event = repository.GetStoredEvents(searchTerm).Last().Should().BeOfType<DiscoveryPlanned>().Subject;

        planned.Should().BeTrue();
        @event.Priority.Should().Be(LookupPriorityBand.High);
        @event.EstimatedRetryAfterSeconds.Should().Be(30);
        @event.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_A_Requested_Discovery_When_Deferring_Then_DiscoveryDeferred_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(searchTerm));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var deferred = discovery.Defer(60, Clock.AddSeconds(60), "Planner deferred lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        var @event = repository.GetStoredEvents(searchTerm).Last().Should().BeOfType<DiscoveryDeferred>().Subject;

        deferred.Should().BeTrue();
        @event.EstimatedRetryAfterSeconds.Should().Be(60);
        @event.Reason.Should().Be("Planner deferred lookup");
    }

    [Fact]
    public async Task Given_A_Discovery_When_Rejecting_Then_DiscoveryRejected_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;

        var rejected = discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        var @event = repository.GetStoredEvents(searchTerm).Should().ContainSingle().Which.Should().BeOfType<DiscoveryRejected>().Subject;

        rejected.Should().BeTrue();
        @event.WillBeLookedUp.Should().BeFalse();
        @event.Reason.Should().Be("Planner rejected lookup");
    }

    [Fact]
    public async Task Given_A_Discovery_When_Failing_Then_DiscoveryFailed_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;

        var failed = discovery.Fail("Lookup failed", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        var @event = repository.GetStoredEvents(searchTerm).Should().ContainSingle().Which.Should().BeOfType<DiscoveryFailed>().Subject;

        failed.Should().BeTrue();
        @event.WillBeLookedUp.Should().BeFalse();
        @event.Reason.Should().Be("Lookup failed");
    }

    [Fact]
    public async Task Given_A_Planned_Discovery_When_A_Lookup_Starts_Then_The_Lookup_Started_Transition_Is_Emitted()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(searchTerm));
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var changed = discovery.LookupStarted(LookupPriorityBand.High, Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        changed.Should().BeTrue();
        repository.GetStoredEvents(searchTerm).Last().Should().BeOfType<DiscoveryStarted>();
    }

    [Fact]
    public async Task Given_A_Planned_Discovery_When_A_Lookup_Fails_Then_The_Lifecycle_Starts_Then_Fails()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(searchTerm));
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var changed = discovery.LookupFailed(LookupPriorityBand.High, "Lookup failed", Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);

        changed.Should().BeTrue();
        repository.GetStoredEvents(searchTerm)[^2].Should().BeOfType<DiscoveryStarted>();
        repository.GetStoredEvents(searchTerm)[^1].Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Rejected_Discovery_When_Planning_Then_The_Transition_Is_Rejected()
    {
        var repository = new CatalogSearchDiscoveryRepositoryFake();
        var searchTerm = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, loaded.Stream, CancellationToken.None);
        loaded = await SearchDiscoveryHistory.LoadAsync(repository, searchTerm, CancellationToken.None);
        discovery = loaded.Aggregate;

        var act = () => discovery.Plan(LookupPriorityBand.Low, 30, null, "Planner queued lookup", Clock);

        act.Should().Throw<InvalidOperationException>();
    }

    private static SearchCatalogRequested Request(MusicSearchCriteria searchCriteria) =>
        new(
            searchCriteria,
            PlaybackProviderFilter.Parse("spotify,appleMusic,youtubeMusic"),
            1,
            10,
            Clock,
            CorrelationId.From("corr-1"));

    private static readonly DateTimeOffset Clock = new(2026, 6, 8, 12, 0, 0, TimeSpan.Zero);
}
