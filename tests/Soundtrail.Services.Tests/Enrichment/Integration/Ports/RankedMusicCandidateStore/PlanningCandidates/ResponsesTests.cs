using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.RankedMusicCandidateStore.RavenEmbedded.PlanningCandidates;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedPortResponsesTests
{
    [Fact]
    public async Task Given_Mixed_Candidates_When_Loading_Planning_Candidates_Then_Only_Pending_Eligible_Candidates_Are_Returned()
    {
        using var env = RankedMusicCandidateStoreTestEnvironment.Create();
        var now = new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero);
        var eligible = new RankedMusicCandidate(MusicCatalogId.From("eligible"), 3, 2, 10, RankedMusicCandidateStatus.Pending, null);
        var ignored = new RankedMusicCandidate(MusicCatalogId.From("ignored"), 3, 2, 10, RankedMusicCandidateStatus.Ignored, null);
        var ineligible = new RankedMusicCandidate(MusicCatalogId.From("ineligible"), 3, 2, 10, RankedMusicCandidateStatus.Pending, now.AddMinutes(5));
        env.Seed(eligible, ignored, ineligible);

        var actual = await env.Store.GetPlanningCandidatesAsync(now, 10, CancellationToken.None);

        actual.Should().BeEquivalentTo([eligible]);
    }
}
