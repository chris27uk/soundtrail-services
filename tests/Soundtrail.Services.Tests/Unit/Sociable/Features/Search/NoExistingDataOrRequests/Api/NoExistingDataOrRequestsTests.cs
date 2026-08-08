using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.Search.NoExistingDataOrRequests.Api;

public sealed class NoExistingDataOrRequestsTests
{
    [Fact]
    public async Task When_Requesting_Then_No_Search_Results_Are_Returned()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response.Should().BeNull();
    }
}

public sealed class RequestUnknownMusicDataMessageEnqueuedTests
{
    [Fact]
    public async Task When_Requesting_Then_The_Search_Query_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.Query
            .Should().Be(environment.SearchCriteria.Query);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Search_Type_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.SearchTypes
            .Should().Be(environment.SearchCriteria.SearchTypes);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Normalised_Identifier_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().SearchCriteria.NormalisedIdentifier
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_High_Priority()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_Full_Trust()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Has_No_Risk()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Time_Is_Set()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 9, 44, 0, TimeSpan.Zero);
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task When_Requesting_Then_The_Message_Id_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task When_Requesting_Then_The_Correlation_Id_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForNoExistingDataOrRequests();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<RequestUnknownMusicDataMessage>().CorrelationId.Should().NotBe(default(CorrelationId));
    }
}
