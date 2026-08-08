using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.NoExistingDataOrRequests.Api;

public sealed class RequestKnownMusicDataMessageEnqueuedTests
{
    [Fact]
    public async Task When_Requesting_Then_The_Operation_Is_Playlist_Tracks()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Operation.Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task When_Requesting_Then_The_Operation_Contains_The_Playlist_Id()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        ((CatalogItemOperation.ChildTracksForPlaylist)environment.SentMessage<RequestKnownMusicDataMessage>().Operation).Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_Full_Trust()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_No_Risk()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 44, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Id_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task When_Requesting_Then_The_Correlation_Id_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId.Should().NotBe(default(CorrelationId));
    }
}
