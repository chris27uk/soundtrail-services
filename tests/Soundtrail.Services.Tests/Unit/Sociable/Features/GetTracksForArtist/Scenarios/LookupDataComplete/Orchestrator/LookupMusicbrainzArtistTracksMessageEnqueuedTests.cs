using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.LookupDataComplete.Orchestrator;

public sealed class LookupMusicbrainzArtistTracksMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessages<DispatchLookupWork>().First().CorrelationId);
    }

    [Fact]
    public async Task Then_The_Created_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 41, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Artist_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).ArtistId.Should().Be(environment.ArtistId);
    }

    private static GetTracksForArtistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistTrackScenarios.MidnightSignals(requestTime));

    private static LookupMusicbrainzArtistTracksMessage Message(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessage<LookupMusicbrainzArtistTracksMessage>();
}
