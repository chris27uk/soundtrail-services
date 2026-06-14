using FluentAssertions;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch.KnownQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchPortContractModes.All), MemberType = typeof(CatalogSearchPortContractModes))]
    public async Task Given_A_Known_Query_When_The_Catalog_Is_Searched_Then_The_Matching_Result_Is_Returned(CatalogSearchPortMode mode)
    {
        using var env = CatalogSearchTestEnvironment.Create(mode);
        env.Seed(ApiKnownTracks.MrBrightsideCatalogTrack());

        var actual = await env.Search.SearchAsync(
            new SearchCatalogCommand(
                "mr brightside",
                SearchTypesFilter.Parse("track"),
                PlaybackProviderFilter.Parse("spotify"),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

        actual.Results.Should().ContainSingle();
        actual.Results[0].Name.Should().Be("Mr. Brightside");
        actual.Results[0].AvailableProviders.Should().ContainSingle(provider => provider == Soundtrail.Contracts.Common.ProviderName.Spotify);
    }
}
