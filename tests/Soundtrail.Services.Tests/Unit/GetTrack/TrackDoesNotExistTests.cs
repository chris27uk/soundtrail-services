using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Unit.GetTrack;

public sealed class TrackDoesNotExistTests
{
    [Fact]
    public async Task Given_A_Missing_Track_When_Requesting_The_Track_Then_No_Track_Is_Returned()
    {
        var environment = GetTrackMissingUnitTestEnvironment.ForMissingTrack();

        var result = await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Given_A_Missing_Track_When_Requesting_The_Track_Then_The_Requested_Track_Id_Is_Read()
    {
        var trackId = TrackId.From("track-402");
        var environment = GetTrackMissingUnitTestEnvironment.ForMissingTrack(trackId);

        await environment.CreateSubjectUnderTest().Handle(environment.CreateRequest());

        environment.Port.RequestedTrackIds.Single().Should().Be(trackId);
    }
}
