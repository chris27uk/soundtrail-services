using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactQuery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactMusicBrainzIdQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownExactMusicBrainzIdQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicCatalogCandidateSearchPortContractModes.All), MemberType = typeof(MusicCatalogCandidateSearchPortContractModes))]
    public async Task Given_A_Known_Exact_MusicBrainz_Id_Query_When_Candidates_Are_Searched_Then_The_Identity_Match_Is_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed("mc_track_1", "mr brightside the killers", title: "Fixture Track", artist: "Fixture Artist", mbid: "f6c30a8f-7d2d-4da8-b3b8-8bbf2b1f6f77");

        var actual = await env.Search.SearchAsync(
            NormalizedSearchQuery.FromText("f6c30a8f-7d2d-4da8-b3b8-8bbf2b1f6f77"),
            CancellationToken.None);

        actual.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                1.00m,
                new MusicCatalogMatchEvidence(
                    true,
                    "fixture track",
                    "fixture artist",
                    string.Empty,
                    string.Empty,
                    "f6c30a8f7d2d4da8b3b88bbf2b1f6f77",
                    null)));
    }
}
