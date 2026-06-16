using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
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
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var discovery = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);
        discovery.Request(Request(criteria));
        await discovery.SaveAsync(env.Repository, CancellationToken.None);

        discovery = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);
        discovery.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);

        var saved = await discovery.SaveAsync(env.Repository, CancellationToken.None);
        var reloaded = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);

        saved.Should().BeTrue();
        reloaded.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock).Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(CatalogSearchDiscoveryRepositoryContractModes.All), MemberType = typeof(CatalogSearchDiscoveryRepositoryContractModes))]
    public async Task Given_Two_Loaded_Copies_When_The_First_Is_Saved_Then_The_Second_Save_Fails_Optimistic_Concurrency(CatalogSearchDiscoveryRepositoryMode mode)
    {
        using var env = CatalogSearchDiscoveryRepositoryTestEnvironment.Create(mode);
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");
        var seed = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);
        seed.Request(Request(criteria));
        await seed.SaveAsync(env.Repository, CancellationToken.None);

        var left = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);
        var right = await CatalogSearchDiscovery.LoadAsync(env.Repository, criteria, CancellationToken.None);
        left.Plan(LookupPriorityBand.High, 30, null, "Planner queued lookup", Clock);
        right.Defer(60, Clock.AddSeconds(60), "Planner deferred lookup", Clock);

        var leftSaved = await left.SaveAsync(env.Repository, CancellationToken.None);
        var rightSaved = await right.SaveAsync(env.Repository, CancellationToken.None);

        leftSaved.Should().BeTrue();
        rightSaved.Should().BeFalse();
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
