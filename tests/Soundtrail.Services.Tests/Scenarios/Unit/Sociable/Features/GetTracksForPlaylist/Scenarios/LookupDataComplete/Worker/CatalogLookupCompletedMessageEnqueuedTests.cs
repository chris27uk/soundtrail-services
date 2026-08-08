using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Value_Is_Playlist_Track_References()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Result(environment).Value.Should().BeOfType<LookedUpData.PlaylistTrackReferences>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_The_Number_Of_Input_Tracks()
    {
        var inputTracks = new[] { InputTrack("First Artist", "First Title"), InputTrack("Second Artist", "Second Title") };
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(inputTracks);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistTracks(environment).Values.Should().HaveCount(inputTracks.Length);
    }

    [Fact]
    public async Task Then_The_Result_Track_Artist_Comes_From_The_Input()
    {
        const string artist = "Completion Input Artist";
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack(artist, "Completion Input Title"));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistTracks(environment).Values.Single().ArtistName.Value.Should().Be(artist);
    }

    [Fact]
    public async Task Then_The_Result_Track_Title_Comes_From_The_Input()
    {
        const string title = "Completion Input Title";
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(InputTrack("Completion Input Artist", title));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistTracks(environment).Values.Single().TrackTitle.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Playlist()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Result(environment).Context.StreamId.StableValue.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }

    [Fact]
    public async Task Then_The_Original_Command_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Result(environment).Context.OriginalCommandId.Should().Be(SpotifyLookup(environment).Id);
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Result(environment).CompletedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 2, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

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

        Message(environment).CorrelationId.Should().Be(SpotifyLookup(environment).CorrelationId);
    }

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(requestTime, InputTrack("Scenario Artist", "Scenario Title", requestTime));

    private static LookupDataCompleteTrack InputTrack(
        string artist,
        string title,
        DateTimeOffset catalogUpdatedAt = default) =>
        LookupDataCompleteTrack.MatchingCatalogTrack(
            artist, title, artist, title, "Scenario Album", new DateOnly(2025, 4, 5), null, 140000, catalogUpdatedAt);

    private static LookupPlaylistTracksByProviderMessage SpotifyLookup(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<LookupPlaylistTracksByProviderMessage>()
            .Single(message => message.Provider == ProviderName.Spotify);

    private static CatalogLookupCompleted Message(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.PlaylistTrackReferences &&
                succeeded.Context.OriginalCommandId == SpotifyLookup(environment).Id);

    private static LookupResult.Succeeded Result(GetTracksForPlaylistSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.PlaylistTrackReferences PlaylistTracks(GetTracksForPlaylistSociableTestEnvironment environment) =>
        (LookedUpData.PlaylistTrackReferences)Result(environment).Value;
}
