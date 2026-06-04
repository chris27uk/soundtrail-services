using FluentAssertions;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.MusicCatalogCandidateSearch.Contract;

public sealed partial class MusicCatalogCandidateSearchPortContractTests
{
    [Theory]
    [MemberData(nameof(Modes))]
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
