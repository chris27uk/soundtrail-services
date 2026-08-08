using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Value_Is_Catalog_Entries()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Result(environment).Value.Should().BeOfType<LookedUpData.CatalogEntries>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_The_Number_Of_Input_Tracks()
    {
        var inputTracks = new[]
        {
            LookupDataCompleteArtistTrackScenarios.MidnightSignals(default),
            LookupDataCompleteArtistTrackScenarios.StaticHearts(default)
        };
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(inputTracks);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        CatalogEntries(environment).Values.Should().HaveCount(inputTracks.Length);
    }

    [Fact]
    public async Task Then_The_Result_Track_Title_Comes_From_The_Input()
    {
        const string title = "Completion Input Title";
        var environment = GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(
            LookupDataCompleteArtistTrack.Create(
                LookupDataCompleteArtistTrackScenarios.DefaultArtistId,
                "Completion Input Artist",
                title,
                "Completion Album",
                new DateOnly(2025, 4, 5),
                null,
                140000,
                default));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        CatalogEntries(environment).Values.Single().Item.Should().BeOfType<CatalogItem.MusicTrack>()
            .Which.Track.Title.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Artist()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Result(environment).Context.StreamId.StableValue
            .Should().Be($"child_tracks_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Then_The_Original_Command_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Result(environment).Context.OriginalCommandId
            .Should().Be(environment.SentMessage<LookupMusicbrainzArtistTracksMessage>().Id);
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Result(environment).CompletedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 2, 0, TimeSpan.Zero);
        var environment = ForCompletedTrack(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedTrack();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetTracksForArtistRequest(environment.ArtistId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessage<LookupMusicbrainzArtistTracksMessage>().CorrelationId);
    }

    private static GetTracksForArtistSociableTestEnvironment ForCompletedTrack(DateTimeOffset requestTime = default) =>
        GetTracksForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistTrackScenarios.MidnightSignals(requestTime));

    private static CatalogLookupCompleted Message(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.OriginalCommandId ==
                    environment.SentMessage<LookupMusicbrainzArtistTracksMessage>().Id);

    private static LookupResult.Succeeded Result(GetTracksForArtistSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(GetTracksForArtistSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
