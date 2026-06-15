using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class SearchOutsideInTests
{
    [Fact]
    public async Task Given_A_Local_Playable_Track_When_Searching_Then_The_Result_Is_Returned_Immediately_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
            SearchOutsideInTestEnvironment.SeedPlayableTrack(
                store,
                "mr brightside",
                "track_mr_brightside",
                "Mr. Brightside",
                "artist_the_killers",
                "The Killers",
                "album_hot_fuss",
                "Hot Fuss",
                ProviderName.Spotify));

        var response = await env.SearchAndWaitForPipelineAsync("mr brightside", types: "track", playback: "spotify");

        response.Query.Should().Be("mr brightside");
        response.Results.Should().ContainSingle();
        response.Results[0].Type.Should().Be("track");
        response.Results[0].Id.Should().Be("track_mr_brightside");
        response.Results[0].Name.Should().Be("Mr. Brightside");
        response.Results[0].ArtistName.Should().Be("The Killers");
        response.Results[0].AlbumName.Should().Be("Hot Fuss");
        response.Results[0].PlayabilityStatus.Should().Be("Playable");
        response.Results[0].AvailableProviders.Should().ContainSingle().Which.Should().Be("spotify");
        response.Discovery.WillBeLookedUp.Should().BeFalse();
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_A_Local_Incomplete_Track_And_Discovery_Status_When_Searching_Then_Current_Results_And_Projected_Discovery_Are_Returned()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            SearchOutsideInTestEnvironment.SeedPlayableTrack(
                store,
                "rare unknown song",
                "track_rare_unknown_song",
                "Rare Unknown Song",
                "artist_test_artist",
                "Test Artist",
                "album_rare_album",
                "Rare Album");
            SearchOutsideInTestEnvironment.SeedDiscoveryStatus(
                store,
                "rare unknown song",
                "track",
                true,
                "Planner queued lookup",
                30);
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Query.Should().Be("rare unknown song");
        response.Results.Should().ContainSingle();
        response.Results[0].Name.Should().Be("Rare Unknown Song");
        response.Results[0].PlayabilityStatus.Should().Be("NotYetDiscovered");
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Planner queued lookup");
        response.Discovery.RetryAfterSeconds.Should().Be(30);
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_No_Local_Result_And_No_Discovery_Status_When_Searching_Then_A_Discovery_Request_Is_Appended_Without_Blocking()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(_ => { });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");
        var lookupRequest = await env.WaitForMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromSeconds(1));
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song").Value;

        response.Query.Should().Be("rare unknown song");
        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Local results incomplete");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        lookupRequest.Query.Should().Be("rare unknown song");
        (await env.HasDiscoveryRequestAsync(criteria)).Should().BeTrue();
        (await env.CountDiscoveryRequestEventsAsync(criteria)).Should().Be(1);
    }

    [Fact]
    public async Task Given_A_Previously_Recorded_Discovery_Request_Without_A_Projection_When_Searching_Then_A_Duplicate_Request_Is_Not_Queued()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(_ => { });
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song").Value;

        await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");
        var firstLookupRequest = await env.WaitForMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromSeconds(1));
        firstLookupRequest.Query.Should().Be("rare unknown song");

        var secondResponse = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        secondResponse.Discovery.WillBeLookedUp.Should().BeTrue();
        secondResponse.Results.Should().BeEmpty();
        env.CountMessages<CatalogSearchAttemptDto>().Should().Be(1);
        (await env.CountDiscoveryRequestEventsAsync(criteria)).Should().Be(1);
    }
}
