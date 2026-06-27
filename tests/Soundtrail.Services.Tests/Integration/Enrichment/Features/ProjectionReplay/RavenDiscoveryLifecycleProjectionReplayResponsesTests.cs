using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus;
using Soundtrail.Services.Internal.Projector.Features.OnReplayCatalogSearchStatus.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using System.Linq;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenDiscoveryLifecycleProjectionReplayResponsesTests
{
    [Fact]
    public async Task Given_Discovery_Lifecycle_Events_When_Replayed_Then_The_Status_Document_Is_Projected()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await SearchOrSeekHistory.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.SearchRequested(Request(criteria));
        await discovery.SaveAsync(repository, CancellationToken.None);

        discovery = await SearchOrSeekHistory.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock.AddSeconds(5));
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var criteriaPersistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteriaPersistentId),
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
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await SearchOrSeekHistory.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Reject("Planner rejected lookup", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var criteriaPersistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteriaPersistentId),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Rejected.ToString());
        status.WillBeLookedUp.Should().BeFalse();
        status.Reason.Should().Be("Planner rejected lookup");
    }

    [Fact]
    public async Task Given_A_Started_Discovery_Event_When_Replayed_Then_The_Status_Document_Is_Projected_As_InProgress()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await SearchOrSeekHistory.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Start(LookupPriorityBand.High, "Lookup started", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var criteriaPersistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteriaPersistentId),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.InProgress.ToString());
        status.WillBeLookedUp.Should().BeTrue();
        status.Reason.Should().Be("Lookup started");
    }

    [Fact]
    public async Task Given_A_Completed_Discovery_Event_When_Replayed_Then_The_Status_Document_Is_Projected_As_Completed()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var repository = new RavenCatalogSearchDiscoveryRepository(raven.Store);
        var discovery = await SearchOrSeekHistory.LoadAsync(repository, criteria, CancellationToken.None);
        discovery.Complete(LookupPriorityBand.High, "Discovery completed", Clock);
        await discovery.SaveAsync(repository, CancellationToken.None);

        await ReplayAsync(raven.Store);

        using var session = raven.Store.OpenAsyncSession();
        var criteriaPersistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(criteria);
        var status = await session.LoadAsync<Soundtrail.Contracts.CatalogSearchStatusRecordDto>(
            Soundtrail.Contracts.CatalogSearchStatusRecordDto.GetDocumentId(criteriaPersistentId),
            CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(CatalogSearchLifecycleStatus.Completed.ToString());
        status.WillBeLookedUp.Should().BeFalse();
        status.Reason.Should().Be("Discovery completed");
    }

    private static async Task ReplayAsync(Raven.Client.Documents.IDocumentStore store)
    {
        using var querySession = store.OpenAsyncSession();
        var streamMetadata = await querySession.Advanced.LoadStartingWithAsync<DiscoveryQueryEventStreamMetadataRecordDto>(
            "discovery-query-streams/");
        var criteriaValues = streamMetadata.Select(x => x.Criteria).ToList();

        using var session = store.OpenAsyncSession();
        var replayHandler = new ReplayCatalogSearchStatusHandler(
            new RavenLoadStoredDiscoveryLifecycleEvents(session),
            new CatalogSearchStatusChangedHandler(
                new RavenLoadDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper()),
                new RavenSaveDiscoveryLifecycleProjection(session, new RavenDiscoveryLifecycleProjectionMapper())));

        foreach (var criteria in criteriaValues.Distinct(StringComparer.Ordinal))
        {
            await replayHandler.Handle(
                new ReplayCatalogSearchStatusCommand(MusicSearchTermPersistentIdTranslator.ToDomainObject(criteria)),
                CancellationToken.None);
        }
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
