using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForPlaylist.LookupDataNotComplete.Projector;

public sealed class AssessWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_For_Playlist_Tracks()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        TargetOperation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForPlaylist>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Contains_The_Playlist_Id()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));
        var operation = (CatalogItemOperation.ChildTracksForPlaylist)TargetOperation(environment);

        operation.Id.Should().Be(environment.PlaylistId);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Has_The_Normalised_Identifier()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().Target.NormalisedIdentifier.Should().Be($"child_tracks_for_playlist:{environment.PlaylistId.Value}");
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_High_Priority()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_Full_Trust()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_No_Risk()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 31, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 32, 0, TimeSpan.Zero);
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Id_Is_Set()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = GetTracksForPlaylistSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForPlaylistRequest(environment.PlaylistId)));

        environment.SentMessage<AssessWorkMessage>().CorrelationId.Should().Be(environment.SentMessage<RequestKnownMusicDataMessage>().CorrelationId);
    }

    private static CatalogItemOperation TargetOperation(GetTracksForPlaylistSociableTestEnvironment environment) =>
        environment.SentMessage<AssessWorkMessage>().Target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation target => target.Operation,
            _ => throw new InvalidOperationException("The fixed scenario should target a known catalog operation.")
        };
}
