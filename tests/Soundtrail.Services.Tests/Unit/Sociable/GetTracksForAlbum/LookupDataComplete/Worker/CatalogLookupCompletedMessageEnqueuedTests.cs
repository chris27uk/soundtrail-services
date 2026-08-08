using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForAlbum.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Value_Is_Catalog_Entries()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Result(environment).Value.Should().BeOfType<LookedUpData.CatalogEntries>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_The_Number_Of_Input_Tracks()
    {
        var inputTracks = new[]
        {
            LookupDataCompleteAlbumTrackScenarios.MidnightSignals(default),
            LookupDataCompleteAlbumTrackScenarios.StaticHearts(default)
        };
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(inputTracks);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        CatalogEntries(environment).Values.Should().HaveCount(inputTracks.Length);
    }

    [Fact]
    public async Task Then_The_Result_Track_Title_Comes_From_The_Input()
    {
        const string title = "Completion Input Title";
        var environment = GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            LookupDataCompleteAlbumTrack.Create(
                LookupDataCompleteAlbumTrackScenarios.DefaultAlbumId,
                "Completion Input Artist",
                title,
                "Completion Album",
                new DateOnly(2025, 4, 5),
                null,
                140000,
                default));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        CatalogEntries(environment).Values.Single().Item.Should().BeOfType<CatalogItem.MusicTrack>()
            .Which.Track.Title.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Artist()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Result(environment).Context.StreamId.StableValue
            .Should().Be($"child_tracks_for_album:{environment.AlbumId.StableValue}");
    }

    [Fact]
    public async Task Then_The_Original_Command_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Result(environment).Context.OriginalCommandId
            .Should().Be(environment.SentMessage<LookupMusicbrainzAlbumTracksMessage>().Id);
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Result(environment).CompletedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 2, 0, TimeSpan.Zero);
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
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessage<LookupMusicbrainzAlbumTracksMessage>().CorrelationId);
    }

    private static GetTracksForAlbumSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForAlbumSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteAlbumTrackScenarios.MidnightSignals(requestTime));

    private static CatalogLookupCompleted Message(GetTracksForAlbumSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.OriginalCommandId ==
                    environment.SentMessage<LookupMusicbrainzAlbumTracksMessage>().Id);

    private static LookupResult.Succeeded Result(GetTracksForAlbumSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(GetTracksForAlbumSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
