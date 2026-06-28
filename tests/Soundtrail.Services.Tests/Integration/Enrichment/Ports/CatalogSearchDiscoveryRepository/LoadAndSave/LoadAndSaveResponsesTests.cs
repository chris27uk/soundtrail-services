using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchDiscoveryRepository.LoadAndSave;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class LoadAndSaveResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchDiscoveryRepositoryContractModes.All), MemberType = typeof(CatalogSearchDiscoveryRepositoryContractModes))]
    public async Task Given_A_Requested_And_Planned_Discovery_When_Loading_Then_The_Aggregate_State_Is_Restored(CatalogSearchDiscoveryRepositoryMode mode)
    {
        using var env = CatalogSearchDiscoveryRepositoryTestEnvironment.Create(mode);
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var loaded = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);
        var discovery = loaded.Aggregate;
        discovery.SearchRequested(Request(criteria));
        await discovery.SaveAsync(env.Repository, loaded.Stream, CancellationToken.None);

        loaded = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);
        discovery = loaded.Aggregate;
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);

        var saved = await discovery.SaveAsync(env.Repository, loaded.Stream, CancellationToken.None);
        var reloaded = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);

        saved.Should().BeTrue();
        reloaded.Aggregate.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(CatalogSearchDiscoveryRepositoryContractModes.All), MemberType = typeof(CatalogSearchDiscoveryRepositoryContractModes))]
    public async Task Given_Two_Loaded_Copies_When_The_First_Is_Saved_Then_The_Second_Save_Fails_Optimistic_Concurrency(CatalogSearchDiscoveryRepositoryMode mode)
    {
        using var env = CatalogSearchDiscoveryRepositoryTestEnvironment.Create(mode);
        var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var seed = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);
        seed.Aggregate.SearchRequested(Request(criteria));
        await seed.Aggregate.SaveAsync(env.Repository, seed.Stream, CancellationToken.None);

        var left = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);
        var right = await SearchOrSeekHistory.LoadAsync(env.Repository, criteria, CancellationToken.None);
        left.Aggregate.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        right.Aggregate.Defer(60, Clock.AddSeconds(60), "Planner deferred lookup", Clock);

        var leftSaved = await left.Aggregate.SaveAsync(env.Repository, left.Stream, CancellationToken.None);
        var rightSaved = await right.Aggregate.SaveAsync(env.Repository, right.Stream, CancellationToken.None);

        leftSaved.Should().BeTrue();
        rightSaved.Should().BeFalse();
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
