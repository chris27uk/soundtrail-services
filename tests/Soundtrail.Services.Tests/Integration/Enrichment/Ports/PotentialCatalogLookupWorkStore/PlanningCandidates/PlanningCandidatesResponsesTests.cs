using FluentAssertions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.PotentialCatalogLookupWorkStore.UpsertedCandidate;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.PotentialCatalogLookupWorkStore.PlanningCandidates;

public sealed class PlanningCandidatesResponsesTests
{
    [Theory]
    [MemberData(nameof(PotentialCatalogLookupWorkStorePortContractModes.All), MemberType = typeof(PotentialCatalogLookupWorkStorePortContractModes))]
    public async Task Given_Mixed_Candidates_When_Loading_Planning_Candidates_Then_Only_Pending_Eligible_Candidates_Are_Returned(PotentialCatalogLookupWorkStorePortMode mode)
    {
        using var env = PotentialCatalogLookupWorkStoreTestEnvironment.Create(mode);
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var eligible = new PotentialCatalogLookupWork(MusicCatalogId.From("eligible"), 3, 2, 10, PotentialCatalogLookupWorkStatus.Pending, null, [DiscoveryQueryKey.Track(TrackId.From("eligible"))]);
        var ignored = new PotentialCatalogLookupWork(MusicCatalogId.From("ignored"), 3, 2, 10, PotentialCatalogLookupWorkStatus.Ignored, null, [DiscoveryQueryKey.Track(TrackId.From("ignored"))]);
        var ineligible = new PotentialCatalogLookupWork(MusicCatalogId.From("ineligible"), 3, 2, 10, PotentialCatalogLookupWorkStatus.Pending, now.AddMinutes(5), [DiscoveryQueryKey.Track(TrackId.From("ineligible"))]);
        env.Seed(eligible, ignored, ineligible);

        var actual = await env.Store.GetPlanningCandidatesAsync(now, 10, CancellationToken.None);

        actual.Should().BeEquivalentTo([eligible]);
    }
}
