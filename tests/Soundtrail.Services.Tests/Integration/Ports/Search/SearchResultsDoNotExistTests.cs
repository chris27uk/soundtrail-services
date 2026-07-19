using Soundtrail.Domain.Search;
using Soundtrail.Services.Api.Features.Search.Contract;

namespace Soundtrail.Services.Tests.Integration.Ports.Search;

public sealed class SearchResultsDoNotExistTests
{
    public static TheoryData<SearchPortImplementation> Implementations => new()
    {
        SearchPortImplementation.Fake,
        SearchPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Missing_Search_Results_When_Searching_Then_No_Search_Results_Are_Returned(SearchPortImplementation implementation)
    {
        await using var environment = await SearchPortContractTestEnvironment.ForMissingResults(implementation, "u2", SearchType.Artist);

        var result = await environment.Subject.SearchAsync(environment.SearchCriteria, CancellationToken.None);

        result.Should().BeNull();
    }
}
