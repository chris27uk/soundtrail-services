using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenDiscoveryLifecycleProjectionReplayResponsesTests
{
    [Fact]
    public async Task Given_Discovery_Lifecycle_Events_When_Replayed_Then_The_Status_Document_Is_Projected()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Request(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);

        discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        status.Priority.Should().Be(LookupPriorityBand.High.ToString());
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.Reason.Should().Be("Planner queued lookup");
    }

    [Fact]
    public async Task Given_A_Rejected_Discovery_Event_When_Replayed_Then_The_Status_Document_Is_Projected_As_Rejected()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Rejected.ToString());
        status.WillBeLookedUp.Should().BeFalse();
        status.Reason.Should().Be("Planner rejected lookup");
    }

    [Fact]
    public async Task Given_An_Artist_Criteria_Discovery_Event_When_Replayed_Then_The_Artist_Status_Document_Is_Projected()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = CatalogSearchCriteria.Artist(ArtistId.From("artist_test_artist"));
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await CatalogSearchDiscovery.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteria.Value),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        status.Reason.Should().Be("Planner queued lookup");
    }

    private static async Task ReplayAsync(Raven.Client.Documents.IDocumentStore store)
    {
        using var session = store.OpenAsyncSession();
        var events = await session.Advanced.AsyncDocumentQuery<DiscoveryQueryStoredEventRecordDto>()
            .ToListAsync(CancellationToken.None);
        var applier = new DiscoveryLifecycleProjectionApplier();

        foreach (var storedEvent in events.OrderBy(x => x.Criteria).ThenBy(x => x.Version))
        {
            await applier.ApplyStoredEventAsync(storedEvent, session, CancellationToken.None);
        }

        await session.SaveChangesAsync(CancellationToken.None);
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
