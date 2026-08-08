using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataNotComplete.Orchestrator;

public sealed class WorkRequestedEventSavedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Operation_Is_For_Playlist_Tracks()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        TargetOperation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Contains_The_Playlist_Id()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));
        var operation = (CatalogItemOperation.ChildTracksForPlaylist)TargetOperation(environment);

        operation.Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Target_Has_The_Normalised_Playlist_Identifier()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().Target.NormalisedIdentifier.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_Full_Trust()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Has_No_Risk()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Event_Request_Time_Is_Saved()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 45, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SavedEvent<WorkRequested>().CorrelationId.Should().Be(environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId);
    }

    private static CatalogItemOperation TargetOperation(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SavedEvent<WorkRequested>().Target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation target => target.Operation,
            _ => throw new InvalidOperationException("The fixed scenario should target a known catalog operation.")
        };
}
