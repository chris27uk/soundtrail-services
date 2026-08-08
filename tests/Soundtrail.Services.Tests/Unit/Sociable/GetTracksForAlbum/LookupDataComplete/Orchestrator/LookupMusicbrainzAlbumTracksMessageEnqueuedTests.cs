using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForAlbum.LookupDataComplete.Orchestrator;

public sealed class LookupMusicbrainzAlbumTracksMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessages<DispatchLookupWork>().First().CorrelationId);
    }

    [Fact]
    public async Task Then_The_Created_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 41, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Album_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).AlbumId.Should().Be(environment.AlbumId);
    }

    private static GetTracksForAlbumSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteAlbumTrackScenarios.MidnightSignals(requestTime));

    private static LookupMusicbrainzAlbumTracksMessage Message(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SentMessage<LookupMusicbrainzAlbumTracksMessage>();
}
