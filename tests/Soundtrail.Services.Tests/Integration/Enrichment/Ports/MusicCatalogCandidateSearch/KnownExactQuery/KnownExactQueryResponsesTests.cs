using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownExactQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicCatalogCandidateSearchPortContractModes.All), MemberType = typeof(MusicCatalogCandidateSearchPortContractModes))]
    public async Task Given_A_Known_Exact_Query_When_Candidates_Are_Searched_Then_The_Matching_Candidate_Is_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed("mc_track_1", "mr brightside", title: "Fixture Track", artist: "Fixture Artist");

        var actual = await env.Search.SearchAsync(
            MusicSearchCriteria.ByQuery("mr brightside", SearchTypesFilter.Tracks),
            CancellationToken.None);

        actual.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                1.00m,
                new MusicCatalogMatchEvidence(
                    false,
                    "fixture track",
                    "fixture artist",
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null)));
    }

    [Theory]
    [MemberData(nameof(MusicCatalogCandidateSearchPortContractModes.All), MemberType = typeof(MusicCatalogCandidateSearchPortContractModes))]
    public async Task Given_A_Query_That_Includes_The_Album_When_Candidates_Are_Searched_Then_The_Album_Aware_Candidate_Is_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed(
            "mc_track_1",
            "rare unknown song test artist",
            title: "Rare Unknown Song",
            artist: "Test Artist",
            albumTitle: "Rare Album");

        var actual = await env.Search.SearchAsync(
            MusicSearchCriteria.ByQuery("rare unknown song test artist rare album", SearchTypesFilter.Tracks),
            CancellationToken.None);

        actual.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                1.00m,
                new MusicCatalogMatchEvidence(
                    false,
                    "rare unknown song",
                    "test artist",
                    "rare album",
                    string.Empty,
                    string.Empty,
                    null)));
    }
}
