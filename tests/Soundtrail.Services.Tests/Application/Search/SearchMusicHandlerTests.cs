using FluentAssertions;
using Soundtrail.Services.Application.Search;
using Soundtrail.Services.Domain.Tracks;

namespace Soundtrail.Services.Tests.Application.Search;

public sealed class SearchMusicHandlerTests
{
    [Fact]
    public async Task Cached_Response_Is_Returned_Immediately()
    {
        var env = SearchMusicTestEnvironment.WithCachedResolvedResponse();

        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForKnownTrack());

        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.Results.Should().ContainSingle();
        env.DemandStore.RecordedQueries.Should().BeEmpty();
    }

    [Fact]
    public async Task Known_Local_Track_Is_Returned_As_Resolved()
    {
        var env = SearchMusicTestEnvironment.WithKnownTrack();

        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForKnownTrack());

        result.Status.Should().Be(ResolutionStatus.Resolved);
        result.Results.Should().ContainSingle();
        result.Results[0].Title.Value.Should().Be("Mr. Brightside");
    }

    [Fact]
    public async Task Unknown_Query_Creates_Demand_And_Returns_Pending()
    {
        var env = SearchMusicTestEnvironment.WithNoKnownTracks();

        var sut = env.CreateHandler();

        var result = await sut.Handle(env.SearchForUnknownTrack());

        result.Status.Should().Be(ResolutionStatus.Pending);
        result.QueryId.Should().NotBeNull();
        env.DemandStore.RecordedQueries.Should().ContainSingle("rare unknown song");
    }
}
