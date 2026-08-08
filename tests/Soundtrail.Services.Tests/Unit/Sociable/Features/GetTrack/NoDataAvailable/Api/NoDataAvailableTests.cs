using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTrack.NoDataAvailable.Api;

public sealed class NoDataAvailableTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Track_Is_Returned()
    {
        var trackId = TestTrackIds.Create("track-402");
        var environment = GetTrackSociableTestEnvironment.ForNoDataAvailable(trackId);

        var result = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Requested_Track_Id_Is_Read()
    {
        var trackId = TestTrackIds.Create("track-402");
        var environment = GetTrackSociableTestEnvironment.ForNoDataAvailable(trackId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.Port.RequestedTrackIds.Single().Should().Be(trackId);
    }

    [Fact]
    public async Task When_Requesting_Then_No_Enrichment_Work_Is_Scheduled()
    {
        var trackId = TestTrackIds.Create("track-402");
        var environment = GetTrackSociableTestEnvironment.ForNoDataAvailable(trackId);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessages.Should().BeEmpty();
    }
}
