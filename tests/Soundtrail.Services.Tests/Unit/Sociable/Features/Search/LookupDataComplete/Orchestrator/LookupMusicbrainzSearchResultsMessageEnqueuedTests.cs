using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.LookupDataComplete.Orchestrator;

public sealed class LookupMusicbrainzSearchResultsMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Query_Is_Set()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).SearchCriteria.Query.Should().Be(environment.SearchCriteria.Query);
    }

    [Fact]
    public async Task Then_The_Search_Type_Is_Set()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).SearchCriteria.SearchTypes.Should().Be(SearchType.Artist);
    }

    [Fact]
    public async Task Then_The_Normalised_Identifier_Is_Set()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).SearchCriteria.NormalisedIdentifier
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task Then_The_Message_Has_High_Priority()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Then_The_Created_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 43, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Requested_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 44, 0, TimeSpan.Zero);
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
            .Should().Be(environment.SentMessages<DispatchLookupWork>()
                .Single(message => message.Target.NormalisedIdentifier == environment.SearchCriteria.NormalisedIdentifier)
                .CorrelationId);
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchScenarios.AuroraLane());

    private static LookupMusicbrainzSearchResultsMessage Message(SearchSociableTestEnvironment environment) =>
        environment.SentMessage<LookupMusicbrainzSearchResultsMessage>();
}
