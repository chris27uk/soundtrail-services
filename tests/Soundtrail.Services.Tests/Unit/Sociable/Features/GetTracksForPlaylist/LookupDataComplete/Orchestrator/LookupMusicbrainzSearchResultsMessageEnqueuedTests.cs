using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete.Orchestrator;

public sealed class LookupMusicbrainzSearchResultsMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Query_Contains_The_Input_Title_And_Artist()
    {
        const string artist = "Input Artist";
        const string title = "Input Title";
        var environment = ForCompletedTrack(artist, title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).SearchCriteria.Query.Should().Be($"{title} {artist}");
    }

    [Fact]
    public async Task Then_The_Search_Type_Is_Track()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).SearchCriteria.SearchTypes.Should().Be(SearchType.Track);
    }

    [Fact]
    public async Task Then_The_Normalised_Identifier_Contains_The_Input_Title_And_Artist()
    {
        const string artist = "Normalised Artist";
        const string title = "Normalised Title";
        var environment = ForCompletedTrack(artist, title);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).SearchCriteria.NormalisedIdentifier.Should().Be($"search:{title} {artist}");
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Created_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 43, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Requested_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 44, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime: requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).CorrelationId.Should().Be(SearchDispatch(environment).CorrelationId);
    }

    private static LookupMusicbrainzSearchResultsMessage Message(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessage<LookupMusicbrainzSearchResultsMessage>();

    private static DispatchLookupWork SearchDispatch(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target is EnrichmentTarget.SearchForUnknownCatalogItem);

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedTrack(
        string artist = "Scenario Artist",
        string title = "Scenario Title",
        DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteTrack.MatchingCatalogTrack(
                artist, title, artist, title, "Input Album", new DateOnly(2025, 1, 2), null, 123456, requestTime));
}
