using FluentAssertions;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch.Contract;

[Collection(RavenEmbeddedCollection.Name)]
public sealed partial class MusicCatalogCandidateSearchPortContractTests
{
    public static IEnumerable<object[]> Modes =>
    [
        [MusicCatalogCandidateSearchPortMode.InProcessFake],
        [MusicCatalogCandidateSearchPortMode.RavenEmbedded]
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Known_Exact_Query_When_Candidates_Are_Searched_Then_The_Matching_Candidate_Is_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed("mc_track_1", "mr brightside");

        var actual = await env.Search.SearchAsync(
            NormalizedSearchQuery.FromText("mr brightside"),
            CancellationToken.None);

        actual.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                1.00m));
    }
}
