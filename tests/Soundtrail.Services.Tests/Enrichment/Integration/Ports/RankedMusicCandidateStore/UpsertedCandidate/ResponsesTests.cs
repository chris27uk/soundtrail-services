using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.RankedMusicCandidateStore.RavenEmbedded.UpsertedCandidate;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedPortResponsesTests
{
    [Fact]
    public async Task Given_An_Upserted_Candidate_When_Loading_By_MusicCatalogId_Then_The_Candidate_Is_Returned()
    {
        using var env = RankedMusicCandidateStoreTestEnvironment.Create();
        var candidate = new RankedMusicCandidate(
            MusicCatalogId.From("mc_track_1"),
            RequestCount: 2,
            HighestTrustLevelSeen: 3,
            RiskScore: 10,
            Status: RankedMusicCandidateStatus.Pending,
            NextEligibleAt: null);

        await env.Store.UpsertAsync(candidate, CancellationToken.None);
        var actual = await env.Store.FindByMusicCatalogIdAsync(MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        actual.Should().Be(candidate);
    }
}
