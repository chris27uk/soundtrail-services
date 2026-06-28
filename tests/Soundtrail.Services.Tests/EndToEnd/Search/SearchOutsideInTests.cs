using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Events;
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
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryPlanned(
                    criteria,
                    LookupPriorityBand.High,
                    true,
                    30,
                    null,
                    "Planner queued lookup",
                    DateTimeOffset.UtcNow));
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
    public async Task Given_A_Local_Incomplete_Track_Without_A_Discovery_Status_When_Searching_Then_The_Local_Result_Is_Returned_And_Discovery_Is_Requested()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
            SearchOutsideInTestEnvironment.SeedPlayableTrack(
                store,
                "rare unknown song",
                "track_rare_unknown_song",
                "Rare Unknown Song",
                "artist_test_artist",
                "Test Artist",
                "album_rare_album",
                "Rare Album"));

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");
        var criteria = DiscoveryQueryKey.StableValueFor(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));

        response.Results.Should().ContainSingle();
        response.Results[0].Id.Should().Be("track_rare_unknown_song");
        response.Results[0].PlayabilityStatus.Should().Be("NotYetDiscovered");
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Local results incomplete");
        (await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150))).Should().BeFalse();
        (await env.HasDiscoveryRequestAsync(criteria)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_An_InProgress_Discovery_Status_When_Searching_Then_The_Projected_Discovery_Is_Returned_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryStarted(
                    criteria,
                    LookupPriorityBand.High,
                    true,
                    "Lookup started",
                    DateTimeOffset.UtcNow));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Lookup started");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_A_Failed_Discovery_Status_When_Searching_Then_The_Projected_Discovery_Is_Returned_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryFailed(
                    criteria,
                    false,
                    "Lookup failed",
                    DateTimeOffset.UtcNow));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeFalse();
        response.Discovery.Reason.Should().Be("Lookup failed");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_A_Deferred_Discovery_Status_When_Searching_Then_The_Projected_Discovery_Is_Returned_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryDeferred(
                    criteria,
                    true,
                    60,
                    null,
                    "Budget temporarily exhausted",
                    DateTimeOffset.UtcNow));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Budget temporarily exhausted");
        response.Discovery.RetryAfterSeconds.Should().Be(60);
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_A_Rejected_Discovery_Status_When_Searching_Then_The_Projected_Discovery_Is_Returned_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryRejected(
                    criteria,
                    false,
                    "Planner rejected lookup",
                    DateTimeOffset.UtcNow));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeFalse();
        response.Discovery.Reason.Should().Be("Planner rejected lookup");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_A_Completed_Discovery_Status_When_Searching_Then_The_Projected_Discovery_Is_Returned_Without_Requesting_Discovery()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedProjectedCatalogSearchStatusFromEvents(
                store,
                criteria,
                new DiscoveryCompleted(
                    criteria,
                    LookupPriorityBand.High,
                    false,
                    "Discovery completed",
                    DateTimeOffset.UtcNow));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeFalse();
        response.Discovery.Reason.Should().Be("Discovery completed");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        var receivedLookupRequest = await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150));
        receivedLookupRequest.Should().BeFalse();
    }

    [Fact]
    public async Task Given_No_Local_Result_And_No_Discovery_Status_When_Searching_Then_A_Discovery_Request_Is_Appended_Without_Blocking()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(_ => { });

        var criteria = DiscoveryQueryKey.StableValueFor(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));
        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        response.Query.Should().Be("rare unknown song");
        response.Results.Should().BeEmpty();
        response.Discovery.WillBeLookedUp.Should().BeTrue();
        response.Discovery.Reason.Should().Be("Local results incomplete");
        response.Discovery.RetryAfterSeconds.Should().BeNull();
        (await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150))).Should().BeFalse();
        (await env.HasDiscoveryRequestAsync(criteria)).Should().BeTrue();
        (await env.CountDiscoveryRequestEventsAsync(criteria)).Should().Be(1);
    }

    [Fact]
    public async Task Given_A_Previously_Recorded_Discovery_Request_Without_A_Projection_When_Searching_Then_A_Duplicate_Request_Is_Not_Queued()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(_ => { });
        var criteria = DiscoveryQueryKey.StableValueFor(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks));

        await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        var secondResponse = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track");

        secondResponse.Discovery.WillBeLookedUp.Should().BeTrue();
        secondResponse.Results.Should().BeEmpty();
        (await env.DidReceiveMessageAsync<CatalogSearchAttemptDto>(TimeSpan.FromMilliseconds(150))).Should().BeFalse();
        (await env.CountDiscoveryRequestEventsAsync(criteria)).Should().Be(1);
    }

    [Fact]
    public async Task Given_Imported_Events_When_Catalog_And_Discovery_Projections_Are_Rebuilt_Then_Search_Returns_Rebuilt_Read_Models()
    {
        await using var env = await SearchOutsideInTestEnvironment.CreateAsync(store =>
        {
            SearchOutsideInTestEnvironment.SeedRebuiltCatalogProjectionFromImportedEvents(
                store,
                MusicCatalogId.From("mc_track_1"),
                new TrackDiscovered("Rare Unknown Song", "Test Artist", 123000, "isrc-1", "mbid-1", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                new ArtistDiscovered("artist_test_artist", "Test Artist", "mb-artist-test-artist", LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)),
                new AlbumDiscovered("album_rare_album", "Rare Album", "mb-release-rare-album", new DateOnly(2026, 1, 1), LookupSource.MusicBrainz, new DateTimeOffset(2026, 6, 16, 12, 2, 0, TimeSpan.Zero)),
                new ProviderReferenceDiscovered(ProviderName.AppleMusic, "apple-track-1", new Uri("https://music.apple.com/track/1"), LookupSource.Odesli, new DateTimeOffset(2026, 6, 16, 12, 3, 0, TimeSpan.Zero)));

            var criteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
            SearchOutsideInTestEnvironment.SeedRebuiltDiscoveryProjectionFromImportedEvents(
                store,
                criteria,
                new DiscoveryRequested(
                    criteria,
                    PlaybackProviderFilter.Parse("appleMusic"),
                    1,
                    10,
                    new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero),
                    CorrelationId.From("corr-1")),
                new DiscoveryCompleted(
                    criteria,
                    LookupPriorityBand.High,
                    false,
                    "Discovery completed",
                    new DateTimeOffset(2026, 6, 16, 12, 5, 0, TimeSpan.Zero)));
        });

        var response = await env.SearchAndWaitForPipelineAsync("rare unknown song", types: "track", playback: "appleMusic");

        response.Results.Should().ContainSingle();
        response.Results[0].Id.Should().Be("mc_track_1");
        response.Results[0].Name.Should().Be("Rare Unknown Song");
        response.Results[0].ArtistName.Should().Be("Test Artist");
        response.Results[0].AlbumName.Should().Be("Rare Album");
        response.Results[0].PlayabilityStatus.Should().Be("Playable");
        response.Results[0].AvailableProviders.Should().ContainSingle().Which.Should().Be("appleMusic");
        response.Discovery.WillBeLookedUp.Should().BeFalse();
        response.Discovery.Reason.Should().Be("Discovery completed");
    }
}
