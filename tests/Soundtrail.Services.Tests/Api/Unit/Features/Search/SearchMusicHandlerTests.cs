using FluentAssertions;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Unit.Features.Search;

public sealed class SearchMusicHandlerTests
{
    [Fact]
    public async Task Given_A_Cached_Response_When_Searching_For_A_Known_Track_Then_The_Cached_Response_Is_Returned()
    {
        var env = SearchMusicTestEnvironment.WithCachedResolvedResponse();
        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForKnownTrack());

        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.Results.Should().ContainSingle();
        env.DemandStore.RecordedQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_A_Known_Local_Track_When_Searching_For_That_Track_Then_A_Resolved_Response_Is_Returned()
    {
        var env = SearchMusicTestEnvironment.WithKnownTrack();

        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForKnownTrack());

        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.Results.Should().ContainSingle();
        result.Results[0].Title.Value.Should().Be("Mr. Brightside");
        env.QueryCache.StoreCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_An_Unknown_Query_When_Searching_For_That_Query_Then_Demand_Is_Recorded_And_A_Pending_Response_Is_Returned()
    {
        var env = SearchMusicTestEnvironment.WithNoKnownTracks();

        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForUnknownTrack());

        result.Status.Should().Be(ResolutionStatus.Pending);
        result.QueryId.Should().NotBeNull();
        env.DemandStore.RecordedQueries.Should().ContainSingle("rare unknown song");
    }
}
