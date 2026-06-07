using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Api.Integration.Ports.TrackSearch.KnownQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(TrackSearchPortContractModes.All), MemberType = typeof(TrackSearchPortContractModes))]
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