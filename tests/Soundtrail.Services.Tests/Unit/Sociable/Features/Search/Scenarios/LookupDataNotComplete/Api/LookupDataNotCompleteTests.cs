namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Scenarios.LookupDataNotComplete.Api;

public sealed class LookupDataNotCompleteTests
{
    [Fact]
    public async Task Given_A_Request_Is_Being_Orchestrated_When_Requesting_Then_No_Search_Results_Are_Returned()
    {
        var environment = await SearchSociableTestEnvironment.ForExistingIncompleteLookup();

        var response = await environment.ProjectOnChange(
            sut => sut.Handle(environment.CreateRequest()));

        response.Should().BeNull();
    }
}
