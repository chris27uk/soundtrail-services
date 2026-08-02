using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete;

public sealed class RequestUnknownMusicDataMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Search_Query_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.Query.Should().Be("Midnight Signals Aurora Lane");
    }

    [Fact]
    public async Task Then_The_Search_Type_Is_Track()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.SearchTypes.Should().Be(SearchType.Track);
    }

    [Fact]
    public async Task Then_The_Normalised_Search_Identifier_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.NormalisedIdentifier.Should().Be("search:Midnight Signals Aurora Lane");
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Message_Has_Full_Trust()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Then_The_Message_Has_No_Risk()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Then_The_Request_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 43, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            MidnightSignals());

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<RequestUnknownMusicDataMessage>().CorrelationId.Should().Be(environment.SentMessages<DispatchLookupWork>().First().CorrelationId);
    }

    private static LookupDataCompleteTrack MidnightSignals() =>
        LookupDataCompleteTrackScenarios.MidnightSignals(default);
}
