using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.RankedMusicCandidateStore.Contract;

[Collection(RavenEmbeddedCollection.Name)]
public sealed partial class RankedMusicCandidateStorePortContractTests
{
    public static IEnumerable<object[]> Modes =>
    [
        [RankedMusicCandidateStorePortMode.InProcessFake],
        [RankedMusicCandidateStorePortMode.RavenEmbedded]
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_An_Upserted_Candidate_When_Loading_By_MusicCatalogId_Then_The_Candidate_Is_Returned(RankedMusicCandidateStorePortMode mode)
    {
        using var env = RankedMusicCandidateStoreTestEnvironment.Create(mode);
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
