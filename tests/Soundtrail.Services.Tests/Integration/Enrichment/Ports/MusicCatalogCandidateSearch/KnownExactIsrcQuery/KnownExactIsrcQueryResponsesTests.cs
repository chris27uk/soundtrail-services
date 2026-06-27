using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactQuery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactIsrcQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownExactIsrcQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicCatalogCandidateSearchPortContractModes.All), MemberType = typeof(MusicCatalogCandidateSearchPortContractModes))]
    public async Task Given_A_Known_Exact_Isrc_Query_When_Candidates_Are_Searched_Then_The_Identity_Match_Is_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed("mc_track_1", "mr brightside the killers", title: "Fixture Track", artist: "Fixture Artist", isrc: "USIR20400274");

        var actual = await env.Search.SearchAsync(
            MusicSearchCriteria.ByQuery("USIR20400274", SearchTypesFilter.Tracks),
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
                    "usir20400274",
                    string.Empty,
                    null)));
    }
}
