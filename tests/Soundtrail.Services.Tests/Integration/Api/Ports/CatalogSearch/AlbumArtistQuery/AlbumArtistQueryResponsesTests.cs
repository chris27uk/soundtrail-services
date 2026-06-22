using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.CatalogBrowsing;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch.AlbumArtistQuery;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class AlbumArtistQueryResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchPortContractModes.All), MemberType = typeof(CatalogSearchPortContractModes))]
    public async Task Given_An_Album_Query_That_Includes_The_Artist_When_The_Catalog_Is_Searched_Then_The_Matching_Album_Is_Returned(
        CatalogSearchPortMode mode)
    {
        using var env = CatalogSearchTestEnvironment.Create(mode);
        env.Seed(new SearchCatalogResult(
            SearchResultType.Album,
            "album_hot_fuss",
            "Hot Fuss",
            "artist_the_killers",
            "The Killers",
            "album_hot_fuss",
            "Hot Fuss",
            PlayabilityStatus.Playable,
            [ProviderName.Spotify],
            [],
            []));

        var actual = await env.Search.SearchAsync(
            new SearchCatalogCommand(
                "hot fuss the killers",
                SearchTypesFilter.Parse("album"),
                PlaybackProviderFilter.Parse("spotify"),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

        actual.Results.Should().ContainSingle();
        actual.Results[0].Type.Should().Be(SearchResultType.Album);
        actual.Results[0].Id.Should().Be("album_hot_fuss");
        actual.Results[0].ArtistName.Should().Be("The Killers");
    }
}
