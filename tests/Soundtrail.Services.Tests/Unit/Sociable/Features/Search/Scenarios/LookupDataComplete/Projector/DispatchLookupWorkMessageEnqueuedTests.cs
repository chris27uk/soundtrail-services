using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;
using Soundtrail.Services.Tests.Unit.Sociable.Features.Search;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LookupDataComplete.Projector;

public sealed class DispatchLookupWorkMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Target_Is_A_Search()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Target.Should().BeOfType<EnrichmentTarget.SearchForUnknownCatalogItem>();
    }

    [Fact]
    public async Task Then_The_Target_Contains_The_Search_Criteria()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));
        var target = (EnrichmentTarget.SearchForUnknownCatalogItem)Message(environment).Target;

        target.Criteria.Should().Be(environment.SearchCriteria);
    }

    [Fact]
    public async Task Then_The_Target_Has_The_Normalised_Search_Identifier()
    {
        var environment = ForCompletedArtist();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Target.NormalisedIdentifier
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
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 31, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).CreatedAt.Should().Be(requestTime);
    }

    [Fact]
    public async Task Then_The_Request_Time_Comes_From_The_Request()
    {
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 32, 0, TimeSpan.Zero);
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
        var requestTime = new DateTimeOffset(2026, 8, 2, 11, 33, 0, TimeSpan.Zero);
        var environment = ForCompletedArtist(requestTime);

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).CorrelationId.Value
            .Should().Be($"work-scheduled:{environment.SearchCriteria.NormalisedIdentifier}:{requestTime:O}");
    }

    private static SearchSociableTestEnvironment ForCompletedArtist(DateTimeOffset requestTime = default) =>
        SearchSociableTestEnvironment.ForLookupDataComplete(
            requestTime,
            LookupDataCompleteSearchScenarios.AuroraLane());

    private static DispatchLookupWork Message(SearchSociableTestEnvironment environment) =>
        environment.SentMessages<DispatchLookupWork>()
            .Single(message => message.Target.NormalisedIdentifier == environment.SearchCriteria.NormalisedIdentifier);
}
