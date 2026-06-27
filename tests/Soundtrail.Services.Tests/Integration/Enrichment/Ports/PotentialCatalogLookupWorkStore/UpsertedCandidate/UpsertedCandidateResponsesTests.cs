using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.PotentialCatalogLookupWorkStore.UpsertedCandidate;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class UpsertedCandidateResponsesTests
{
    [Theory]
    [MemberData(nameof(PotentialCatalogLookupWorkStorePortContractModes.All), MemberType = typeof(PotentialCatalogLookupWorkStorePortContractModes))]
    public async Task Given_An_Upserted_Candidate_When_Loading_By_MusicCatalogId_Then_The_Candidate_Is_Returned(PotentialCatalogLookupWorkStorePortMode mode)
    {
        using var env = PotentialCatalogLookupWorkStoreTestEnvironment.Create(mode);
        var candidate = new PotentialCatalogLookupWork(
            MusicCatalogId.From("mc_track_1"),
            RequestCount: 2,
            HighestTrustLevelSeen: 3,
            RiskScore: 10,
            Status: PotentialCatalogLookupWorkStatus.Pending,
            NextEligibleAt: null);

        await env.Store.UpsertAsync(candidate, CancellationToken.None);
        var actual = await env.Store.FindByMusicCatalogIdAsync(MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        actual.Should().BeEquivalentTo(candidate);
    }
}
