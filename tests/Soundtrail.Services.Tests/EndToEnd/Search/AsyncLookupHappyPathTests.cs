using FluentAssertions;
using Soundtrail.Services.Api.Features.Search.TrackSearch;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class AsyncLookupHappyPathTests
{
    [Fact]
    public async Task Given_A_Missing_Search_Result_When_The_Local_Wolverine_Pipeline_Completes_Then_A_Requery_Returns_The_Track()
    {
        await using var env = await AsyncLookupHappyPathTestEnvironment.CreateAsync();

        var first = await env.SearchAndWaitForPipelineAsync("rare unknown song", TimeSpan.FromSeconds(5));

        first.Status.Should().Be(ResolutionStatus.Pending);
        first.Results.Should().BeEmpty();

        var resolved = await env.WaitForPlayableSearchAsync("rare unknown song", TimeSpan.FromSeconds(15));

        resolved.Status.Should().Be(ResolutionStatus.Resolved);
        resolved.Results.Should().ContainSingle();
        resolved.Results[0].Title.Value.Should().Be("Rare Unknown Song");
        resolved.Results[0].Artist.Value.Should().Be("Test Artist");
        resolved.Results[0].AppleId!.Value.Should().Be("apple-track-1");
    }
}
