using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.NoResultsFound.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Result_Is_Succeeded()
    {
        var environment = SearchSociableTestEnvironment.ForNoResultsFound();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Message(environment).Result.Should().BeOfType<LookupResult.Succeeded>();
    }

    [Fact]
    public async Task Then_The_Result_Contains_No_Catalog_Entries()
    {
        var environment = SearchSociableTestEnvironment.ForNoResultsFound();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        CatalogEntries(environment).Values.Should().BeEmpty();
    }

    [Fact]
    public async Task Then_The_Result_Stream_Id_Targets_The_Search()
    {
        var environment = SearchSociableTestEnvironment.ForNoResultsFound();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        Result(environment).Context.StreamId.StableValue
            .Should().Be(environment.SearchCriteria.NormalisedIdentifier);
    }

    private static CatalogLookupCompleted Message(SearchSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message => message.Result is LookupResult.Succeeded succeeded &&
                succeeded.Value is LookedUpData.CatalogEntries &&
                succeeded.Context.StreamId.StableValue == environment.SearchCriteria.NormalisedIdentifier);

    private static LookupResult.Succeeded Result(SearchSociableTestEnvironment environment) =>
        (LookupResult.Succeeded)Message(environment).Result;

    private static LookedUpData.CatalogEntries CatalogEntries(SearchSociableTestEnvironment environment) =>
        (LookedUpData.CatalogEntries)Result(environment).Value;
}
