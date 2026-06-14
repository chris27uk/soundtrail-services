using FluentAssertions;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class AsyncLookupHappyPathTests(AsyncLookupHappyPathScenario scenario) : IClassFixture<AsyncLookupHappyPathScenario>
{
    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_First_Search_Response_When_Inspecting_The_Status_Then_It_Is_Pending()
    {
        scenario.FirstSearchResponse.Status.Should().Be("pending");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_First_Search_Response_When_Inspecting_The_Results_Then_It_Is_Empty()
    {
        scenario.FirstSearchResponse.Results.Should().BeEmpty();
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Search_Request_Message_When_Inspecting_The_Query_Then_It_Is_Captured()
    {
        scenario.LookupMusicRequest.Query.Should().Be("rare unknown song");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Canonical_Metadata_Command_When_Inspecting_The_MusicCatalogId_Then_It_Is_Captured()
    {
        scenario.LookupCanonicalMusicMetadataCommand.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Enrichment_Response_When_Inspecting_The_MusicCatalogId_Then_It_Is_Captured()
    {
        scenario.EnrichmentResponse.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Playback_Resolution_Required_Message_When_Inspecting_The_MusicCatalogId_Then_It_Is_Captured()
    {
        scenario.PlaybackReferencesResolutionRequired.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Playback_Resolution_Command_When_Inspecting_The_MusicCatalogId_Then_It_Is_Captured()
    {
        scenario.ResolvePlaybackReferencesCommand.MusicCatalogId.Should().Be("mc_track_1");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Resolved_Search_Response_When_Inspecting_The_Status_Then_It_Is_Resolved()
    {
        scenario.ResolvedSearchResponse.Status.Should().Be("resolved");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Resolved_Search_Response_When_Inspecting_The_Result_Count_Then_It_Contains_A_Single_Result()
    {
        scenario.ResolvedSearchResponse.Results.Should().ContainSingle();
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Resolved_Search_Response_When_Inspecting_The_Title_Then_It_Is_Returned()
    {
        scenario.ResolvedSearchResponse.Results[0].Title.Should().Be("Rare Unknown Song");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Resolved_Search_Response_When_Inspecting_The_Artist_Then_It_Is_Returned()
    {
        scenario.ResolvedSearchResponse.Results[0].Artist.Should().Be("Test Artist");
    }

    [Fact(Skip = "Obsolete POC search contract. Replace with spec-aligned outside-in coverage after catalog projection flow is in place.")]
    public void Given_The_Resolved_Search_Response_When_Inspecting_The_AppleId_Then_It_Is_Returned()
    {
        scenario.ResolvedSearchResponse.Results[0].AppleId.Should().Be("apple-track-1");
    }
}
