using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Features.Search;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch.PlaybackFiltering;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class PlaybackFilteringResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchPortContractModes.All), MemberType = typeof(CatalogSearchPortContractModes))]
    public async Task Given_A_Playback_Filter_When_No_Requested_Provider_Is_Available_Then_No_Results_Are_Returned(CatalogSearchPortMode mode)
    {
        using var env = CatalogSearchTestEnvironment.Create(mode);
        env.Seed(ApiKnownTracks.TheKillersArtist());

        var actual = await env.Search.SearchAsync(
            new SearchCatalogCommand(
                "the killers",
                SearchTypesFilter.Parse("artist"),
                PlaybackProviderFilter.Parse("youtubeMusic"),
                SearchLimit.From(25),
                SearchOffset.From(0)),
            CancellationToken.None);

        actual.Results.Should().BeEmpty();
    }
}
