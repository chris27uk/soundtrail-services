using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataComplete;

public sealed class AssessWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Playlist_Tracks()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistOperation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        ((CatalogItemOperation.ChildTracksForPlaylist)PlaylistOperation(environment)).Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Playlist_Id()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).Target.NormalisedIdentifier.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Message_Has_Full_Trust()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Then_The_Message_Has_No_Risk()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Then_The_Created_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 17, 0, TimeSpan.Zero);
        var environment = ForCompletedPlaylistLookup(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 18, 0, TimeSpan.Zero);
        var environment = ForCompletedPlaylistLookup(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedPlaylistLookup();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        PlaylistAssessment(environment).CorrelationId.Should().Be(PlaylistRequest(environment).CorrelationId);
    }

    private static GetTracksForPlaylistSociableTestEnvironment ForCompletedPlaylistLookup(DateTimeOffset requestTime = default) =>
        GetTracksForPlaylistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteTrackScenarios.MidnightSignals(default));

    private static AssessWorkMessage PlaylistAssessment(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<AssessWorkMessage>()
            .Single(message => message.Target.NormalisedIdentifier == $"child_tracks_for_playlist:{environment.PlaylistId.Value}");

    private static CatalogItemOperation PlaylistOperation(GetTracksForPlaylistSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)PlaylistAssessment(environment).Target).Operation;

    private static RequestKnownMusicDataMessage PlaylistRequest(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessages<RequestKnownMusicDataMessage>()
            .Single(message => message.Operation is CatalogItemOperation.ChildTracksForPlaylist);
}
