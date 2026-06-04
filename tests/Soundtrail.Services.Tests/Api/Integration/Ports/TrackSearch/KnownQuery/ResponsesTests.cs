using FluentAssertions;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.Contract;

[Collection(RavenEmbeddedCollection.Name)]
public sealed partial class TrackSearchPortContractTests
{
    public static IEnumerable<object[]> Modes =>
    [
        [TrackSearchPortMode.InProcessFake],
        [TrackSearchPortMode.RavenEmbedded]
    ];

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Known_Query_When_Tracks_Are_Searched_Then_The_Matching_Result_Is_Returned(TrackSearchPortMode mode)
    {
        using var env = TrackSearchTestEnvironment.Create(mode);
        env.Seed(TrackSearchKnownResults.MrBrightside());

        var actual = await env.Search.SearchAsync(
            NormalizedSearchQuery.FromText("mr brightside"),
            Limit.From(10),
            CancellationToken.None);

        actual.Should().BeEquivalentTo([TrackSearchKnownResults.MrBrightsideFromIndex()]);
    }
}
