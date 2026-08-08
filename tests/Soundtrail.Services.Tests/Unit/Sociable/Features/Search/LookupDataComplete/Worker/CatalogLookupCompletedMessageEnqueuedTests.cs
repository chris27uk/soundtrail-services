using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Value_Is_Catalog_Entries()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Result(environment).Value.Should().BeOfType<LookedUpData.CatalogEntries>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_The_Number_Of_Input_Artists()
    {
        var inputArtists = new[]
        {
            LookupDataCompleteSearchScenarios.AuroraLane(),
            LookupDataCompleteSearchArtist.Create(
                ArtistId.From("artist-aurora-lane-alias"),
                LookupDataCompleteSearchScenarios.DefaultQuery)
        };
        var environment = SearchSociableTestEnvironment.ForLookupDataComplete(inputArtists);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        CatalogEntries(environment).Values.Should().HaveCount(inputArtists.Length);
    }

    [Fact]
    public async Task Then_The_Result_Artist_Name_Comes_From_The_Input()
    {
        const string name = "Completion Input Artist";
        var environment = SearchSociableTestEnvironment.ForLookupDataComplete(
            LookupDataCompleteSearchArtist.Create(
                ArtistId.From("artist-completion-input"),
                name));

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        CatalogEntries(environment).Values.Single().Item.Should().BeOfType<CatalogItem.MusicArtist>()
            .Which.Artist.Name.Value.Should().Be(name);
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Search()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Result(environment).Context.StreamId.StableValue
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task Then_The_Original_Command_Id_Is_Preserved()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Result(environment).Context.OriginalCommandId
            .Should().Be(environment.SentMessage<LookupMusicbrainzSearchResultsMessage>().Id);
    }

    [Fact]
    public async Task Then_The_Completed_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 1, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Result(environment).CompletedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 12, 2, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Message_Id_Is_Set()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).CorrelationId
            .Should().Be(environment.SentMessage<LookupMusicbrainzSearchResultsMessage>().CorrelationId);
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchScenarios.AuroraLane());

    private static CatalogLookupCompleted Message(SearchSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.OriginalCommandId ==
                    environment.SentMessage<LookupMusicbrainzSearchResultsMessage>().Id);

    private static LookupResult.Succeeded Result(SearchSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(SearchSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
