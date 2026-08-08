using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForPlaylist.Scenarios.LookupDataComplete.Projector;

public sealed class DispatchLookupWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Playlist_Tracks()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        ((CatalogItemOperation.ChildTracksForPlaylist)Operation(environment)).Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).Target.NormalisedIdentifier.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
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
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 31, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 32, 0, TimeSpan.Zero);
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
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 33, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        Message(environment).CorrelationId.Value.Should().Be($"work-scheduled:child_tracks_for_playlist:{environment.PlaylistId.Value}:{requestTime:O}");
    }

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteTrackScenarios.MidnightSignals(requestTime));

    private static DispatchLookupWork Message(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier == $"child_tracks_for_playlist:{environment.PlaylistId.Value}");

    private static CatalogItemOperation Operation(GetTracksForPlaylistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Message(environment).Target).Operation;
}
