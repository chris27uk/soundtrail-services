using FluentAssertions;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.Contract;

public sealed partial class TrackSearchPortContractTests
{
    [Theory]
    [MemberData(nameof(Modes))]
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
