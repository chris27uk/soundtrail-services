using FluentAssertions;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Persistence;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.RankedMusicCandidateStore.UpsertedCandidate;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.RankedMusicCandidateStore.PlanningCandidates;

public sealed class PlanningCandidatesResponsesTests
{
    [Theory]
    [MemberData(nameof(RankedMusicCandidateStorePortContractModes.All), MemberType = typeof(RankedMusicCandidateStorePortContractModes))]
    public async Task Given_Mixed_Candidates_When_Loading_Planning_Candidates_Then_Only_Pending_Eligible_Candidates_Are_Returned(RankedMusicCandidateStorePortMode mode)
    {
        using var env = RankedMusicCandidateStoreTestEnvironment.Create(mode);
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var eligible = new RankedMusicCandidate(MusicCatalogId.From("eligible"), 3, 2, 10, RankedMusicCandidateStatus.Pending, null);
        var ignored = new RankedMusicCandidate(MusicCatalogId.From("ignored"), 3, 2, 10, RankedMusicCandidateStatus.Ignored, null);
        var ineligible = new RankedMusicCandidate(MusicCatalogId.From("ineligible"), 3, 2, 10, RankedMusicCandidateStatus.Pending, now.AddMinutes(5));
        env.Seed(eligible, ignored, ineligible);

        var actual = await env.Store.GetPlanningCandidatesAsync(now, 10, CancellationToken.None);

        actual.Should().BeEquivalentTo([eligible]);
    }
}
