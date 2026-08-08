using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForAlbum.Scenarios.LookupDataComplete.Projector;

public sealed class DispatchLookupWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Known_Catalog_Operation()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Target.Should().BeOfType<EnrichmentTarget.KnownCatalogItemOperation>();
    }

    [Fact]
    public async Task Then_The_Target_Operation_Is_Album_Tracks()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Operation(environment).Should().BeOfType<CatalogItemOperation.ChildTracksForAlbum>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Album_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        ((CatalogItemOperation.ChildTracksForAlbum)Operation(environment)).Id.Should().Be(environment.AlbumId);
    }

    [Fact]
    public async Task Then_The_Target_Normalised_Identifier_Contains_The_Album_Id()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Target.NormalisedIdentifier
            .Should().Be($"child_tracks_for_album:{environment.AlbumId.StableValue}");
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Created_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 31, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 32, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 33, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).CorrelationId.Value
            .Should().Be($"work-scheduled:child_tracks_for_album:{environment.AlbumId.StableValue}:{requestTime:O}");
    }

    private static GetTracksForAlbumSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteAlbumTrackScenarios.MidnightSignals(requestTime));

    private static DispatchLookupWork Message(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier == $"child_tracks_for_album:{environment.AlbumId.StableValue}");

    private static CatalogItemOperation Operation(GetTracksForAlbumSociableTestEnvironment environment) =>
        ((EnrichmentTarget.KnownCatalogItemOperation)Message(environment).Target).Operation;
}
