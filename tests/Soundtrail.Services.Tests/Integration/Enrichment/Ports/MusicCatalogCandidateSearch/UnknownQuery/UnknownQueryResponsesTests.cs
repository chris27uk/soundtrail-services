using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.KnownExactQuery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.MusicCatalogCandidateSearch.UnknownQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class UnknownQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(MusicCatalogCandidateSearchPortContractModes.All), MemberType = typeof(MusicCatalogCandidateSearchPortContractModes))]
    public async Task Given_An_Unknown_Query_When_Candidates_Are_Searched_Then_No_Candidates_Are_Returned(MusicCatalogCandidateSearchPortMode mode)
    {
        using var env = MusicCatalogCandidateSearchTestEnvironment.Create(mode);
        env.Seed("mc_track_1", "mr brightside");

        var actual = await env.Search.SearchAsync(
            NormalizedSearchQuery.FromText("completely unknown"),
            CancellationToken.None);

        actual.Should().BeEmpty();
    }
}
