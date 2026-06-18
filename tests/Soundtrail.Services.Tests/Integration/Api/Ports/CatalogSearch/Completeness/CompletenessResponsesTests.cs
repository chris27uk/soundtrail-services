using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch.Completeness;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class CompletenessResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchPortContractModes.All), MemberType = typeof(CatalogSearchPortContractModes))]
    public async Task Given_A_Local_Result_That_Is_Not_Yet_Discovered_When_The_Catalog_Is_Searched_Then_The_Response_Is_Not_Complete(CatalogSearchPortMode mode)
    {
        using var env = CatalogSearchTestEnvironment.Create(mode);
        env.Seed(
            new SearchCatalogResult(
                SearchResultType.Track,
                "track_rare_unknown_song",
                "Rare Unknown Song",
                "artist_test_artist",
                "Test Artist",
                "album_rare_album",
                "Rare Album",
                PlayabilityStatus.NotYetDiscovered,
                [],
                [],
                []));

        var actual = await env.Search.SearchAsync(
            new SearchCatalogCommand(
                "rare unknown song",
                SearchTypesFilter.Parse("track"),
                PlaybackProviderFilter.Parse(null),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

        actual.Results.Should().ContainSingle();
        actual.IsComplete.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(CatalogSearchPortContractModes.All), MemberType = typeof(CatalogSearchPortContractModes))]
    public async Task Given_A_Playable_Track_Without_Provider_References_When_The_Catalog_Is_Searched_Then_The_Response_Is_Not_Complete(CatalogSearchPortMode mode)
    {
        using var env = CatalogSearchTestEnvironment.Create(mode);
        env.Seed(
            new SearchCatalogResult(
                SearchResultType.Track,
                "track_rare_unknown_song",
                "Rare Unknown Song",
                "artist_test_artist",
                "Test Artist",
                "album_rare_album",
                "Rare Album",
                PlayabilityStatus.Playable,
                [ProviderName.Spotify],
                [],
                []));

        var actual = await env.Search.SearchAsync(
            new SearchCatalogCommand(
                "rare unknown song",
                SearchTypesFilter.Parse("track"),
                PlaybackProviderFilter.Parse("spotify"),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

        actual.Results.Should().ContainSingle();
        actual.IsComplete.Should().BeFalse();
    }
}
