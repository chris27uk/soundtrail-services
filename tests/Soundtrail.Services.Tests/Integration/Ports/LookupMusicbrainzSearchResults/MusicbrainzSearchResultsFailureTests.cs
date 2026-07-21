using System.Net;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Ports.LookupMusicbrainzSearchResults;

public sealed class MusicbrainzSearchResultsFailureTests
{
    [Fact]
    public async Task Given_A_Connectivity_Failure_When_Reading_Then_The_Exception_Is_Propagated()
    {
        using var environment = ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment.ForConnectivityFailure();

        var action = () => environment.Subject.ReadAsync(new SearchCriteria("rare song", SearchType.Track), CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Given_Malformed_Json_When_Reading_Then_An_Exception_Is_Thrown()
    {
        using var environment = ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment.ForMalformedJson();

        var action = () => environment.Subject.ReadAsync(new SearchCriteria("rare song", SearchType.Track), CancellationToken.None);

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Given_An_Unexpected_Response_Contract_When_Reading_Then_An_Invalid_Operation_Exception_Is_Thrown()
    {
        using var environment = ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment.ForUnexpectedContract();

        var action = () => environment.Subject.ReadAsync(new SearchCriteria("rare song", SearchType.Artist), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("MusicBrainz artist search response must include artists.");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Given_A_Non_Success_Status_Code_When_Reading_Then_An_Http_Request_Exception_Is_Thrown(HttpStatusCode statusCode)
    {
        using var environment = ReadCatalogEntriesBySearchCriteriaPortContractTestEnvironment.ForHttpStatusCode(statusCode);

        var action = () => environment.Subject.ReadAsync(new SearchCriteria("rare song", SearchType.Track), CancellationToken.None);

        await action.Should().ThrowAsync<HttpRequestException>();
    }
}
