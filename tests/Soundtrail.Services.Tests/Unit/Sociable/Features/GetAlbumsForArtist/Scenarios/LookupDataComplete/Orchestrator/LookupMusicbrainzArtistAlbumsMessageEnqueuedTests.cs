using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.LookupDataComplete.Orchestrator;

public sealed class LookupMusicbrainzArtistAlbumsMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessages<DispatchLookupWork>().First().CorrelationId);
    }

    [Fact]
    public async Task Then_The_Created_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 41, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Artist_Id_Is_Set()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).ArtistId.Should().Be(environment.ArtistId);
    }

    private static GetAlbumsForArtistSociableTestEnvironment ForCompletedAlbum(DateTimeOffset requestTime = default) =>
        GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistAlbumScenarios.MidnightSignals(requestTime));

    private static LookupMusicbrainzArtistAlbumsMessage Message(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SentMessage<LookupMusicbrainzArtistAlbumsMessage>();
}
