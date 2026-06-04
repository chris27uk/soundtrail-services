using FluentAssertions;
using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.KnownQuery;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.UnknownQuery;

public sealed class UnknownQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(TrackSearchPortContractModes.All), MemberType = typeof(TrackSearchPortContractModes))]
    public async Task Given_An_Unknown_Query_When_Tracks_Are_Searched_Then_No_Results_Are_Returned(TrackSearchPortMode mode)
    {
        using var env = TrackSearchTestEnvironment.Create(mode);
        env.Seed(TrackSearchKnownResults.MrBrightside());

        var actual = await env.Search.SearchAsync(
            NormalizedSearchQuery.FromText("completely unknown"),
            Limit.From(10),
            CancellationToken.None);

        actual.Should().BeEmpty();
    }
}
