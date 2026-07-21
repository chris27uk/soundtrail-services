using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsHandlerTests
{
    [Fact]
    public async Task Given_Search_Results_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupMusicbrainzSearchResultsUnitTestEnvironment.Create();
        var request = environment.CreateRequest(searchType: SearchType.All);
        var subject = environment.CreateBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        environment.ReadCatalogEntriesBySearchCriteriaPort.RequestedSearchCriteria.Should().Be(request.SearchCriteria);
        var completed = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        var result = completed.Result.Should().BeOfType<LookupResult.Succeeded>().Subject;
        var entries = result.Value.Should().BeOfType<LookedUpData.CatalogEntries>().Subject;
        entries.Values.Should().HaveCount(3);
    }
}
