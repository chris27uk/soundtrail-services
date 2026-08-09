namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.NoResultsFound.Api;

public sealed class NoResultsFoundTests
{
    [Fact]
    public async Task Given_Lookup_Found_No_Results_When_Requesting_Then_No_Search_Results_Are_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingCompletedEmptyLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response.Should().BeNull();
    }
}
