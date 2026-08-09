using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LookupDataNotComplete.Projector;

public sealed class AssessWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Is_A_Search()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().Target.Should().BeOfType<EnrichmentTarget.SearchForUnknownCatalogItem>();
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Contains_The_Search_Criteria()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));
        var target = (EnrichmentTarget.SearchForUnknownCatalogItem)environment.SentMessage<AssessWorkMessage>().Target;

        target.Criteria.Should().Be(environment.SearchCriteria);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Target_Has_The_Normalised_Identifier()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().Target.NormalisedIdentifier
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_High_Priority()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().Priority.Should().Be(LookupPriorityBand.High);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_Full_Trust()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().TrustLevel.Should().Be(100);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Has_No_Risk()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().RiskScore.Should().Be(0);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 31, 0, TimeSpan.Zero);
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 10, 32, 0, TimeSpan.Zero);
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().RequestedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Assessment_Id_Is_Set()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().Id.Should().NotBe(default(MessageId));
    }

    [Fact]
    public async Task Given_The_Request_Is_Orchestrated_Then_The_Correlation_Id_Is_Preserved()
    {
        var environment = SearchSociableTestEnvironment.ForLookupDataNotComplete();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        environment.SentMessage<AssessWorkMessage>().CorrelationId
            .Should().Be(environment.SentMessage<RequestUnknownMusicDataMessage>().CorrelationId);
    }
}
