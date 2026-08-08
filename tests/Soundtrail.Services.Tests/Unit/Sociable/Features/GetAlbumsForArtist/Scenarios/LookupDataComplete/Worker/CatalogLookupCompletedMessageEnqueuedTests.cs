using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Scenarios.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Value_Is_Catalog_Entries()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Result(environment).Value.Should().BeOfType<LookedUpData.CatalogEntries>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_The_Number_Of_Input_Albums()
    {
        var inputAlbums = new[]
        {
            LookupDataCompleteArtistAlbumScenarios.MidnightSignals(default),
            LookupDataCompleteArtistAlbumScenarios.StaticHearts(default)
        };
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(inputAlbums);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        CatalogEntries(environment).Values.Should().HaveCount(inputAlbums.Length);
    }

    [Fact]
    public async Task Then_The_Result_Album_Title_Comes_From_The_Input()
    {
        const string title = "Completion Input Title";
        var environment = GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            LookupDataCompleteArtistAlbum.Create(
                LookupDataCompleteArtistAlbumScenarios.DefaultArtistId,
                title,
                new DateOnly(2025, 4, 5),
                default,
                sourceAlbumId: "completion-input"));

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        CatalogEntries(environment).Values.Single().Item.Should().BeOfType<CatalogItem.MusicAlbum>()
            .Which.Album.AlbumTitle.Should().Be(title);
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Artist()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Result(environment).Context.StreamId.StableValue
            .Should().Be($"child_albums_for_artist:{environment.ArtistId.Value}");
    }

    [Fact]
    public async Task Then_The_Original_Command_Id_Is_Preserved()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Result(environment).Context.OriginalCommandId
            .Should().Be(environment.SentMessage<LookupMusicbrainzArtistAlbumsMessage>().Id);
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Result(environment).CompletedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 2, 0, TimeSpan.Zero);
        var environment = ForCompletedAlbum(requestTime);

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedAlbum();

        await environment.ProjectOnChange(
            sut => sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessage<LookupMusicbrainzArtistAlbumsMessage>().CorrelationId);
    }

    private static GetAlbumsForArtistSociableTestEnvironment ForCompletedAlbum(DateTimeOffset requestTime = default) =>
        GetAlbumsForArtistSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteArtistAlbumScenarios.MidnightSignals(requestTime));

    private static CatalogLookupCompleted Message(GetAlbumsForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.OriginalCommandId ==
                    environment.SentMessage<LookupMusicbrainzArtistAlbumsMessage>().Id);

    private static LookupResult.Succeeded Result(GetAlbumsForArtistSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(GetAlbumsForArtistSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
