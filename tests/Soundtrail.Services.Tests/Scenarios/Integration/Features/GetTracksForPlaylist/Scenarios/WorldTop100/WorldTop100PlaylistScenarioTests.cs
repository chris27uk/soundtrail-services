using Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist;
namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForPlaylist.Scenarios.WorldTop100;

public sealed class WorldTop100PlaylistScenarioTests
{
    [Fact]
    public async Task Given_A_Local_WireMock_WorldTop100_Scenario_When_Requesting_And_Retrying_Then_Timing_And_Fuzzy_Matched_Metadata_Are_Returned()
    {
        await using var environment = await WorldTop100PlaylistScenarioTestEnvironment.CreateAsync();
        await environment.SeedPendingDiscoveryAsync();

        var pending = await environment.GetPlaylistAsync();

        pending.Should().NotBeNull();
        pending!.PlaylistId.Should().Be("worldtop100");
        pending.Tracks.Should().BeEmpty();
        pending.Discovery.Should().NotBeNull();
        pending.Discovery!.Status.Should().Be("scheduled");
        pending.Discovery.NextEligibleAtUtc.Should().NotBeNull();
        pending.Discovery.EarliestExpectedCompletionAtUtc.Should().NotBeNull();

        var streamingCoverage = await environment.MaterializeResolvedScenarioAsync();

        var resolved = await environment.GetPlaylistAsync();

        resolved.Should().NotBeNull();
        resolved!.Tracks.Should().HaveCount(4);
        resolved.Tracks.Select(track => track.Title)
            .Should()
            .BeEquivalentTo(["Midnight Signals", "Static Hearts", "Glass Cities (Radio Edit)", "Golden Echo - Radio Edit"]);
        resolved.Tracks.Select(track => track.ArtistName)
            .Should()
            .BeEquivalentTo(["Aurora Lane", "Paper Tigers", "Neon Harbour", "Saturn Kids"]);
        resolved.Tracks.Where(track => track.Playable)
            .Select(track => track.Title)
            .Should()
            .BeEquivalentTo(["Midnight Signals", "Glass Cities (Radio Edit)"]);
        resolved.Tracks.Where(track => !track.Playable)
            .Select(track => track.Title)
            .Should()
            .BeEquivalentTo(["Static Hearts", "Golden Echo - Radio Edit"]);
        resolved.Tracks
            .SelectMany(track => track.StreamingLocations)
            .Select(location => location.Url)
            .Should()
            .BeEquivalentTo([
                "https://open.spotify.com/track/midnight-signals",
                "https://music.youtube.com/watch?v=glass-cities-radio"
            ]);
        resolved.Discovery.Should().NotBeNull();
        resolved.Discovery!.Status.Should().Be("completed");
        streamingCoverage.ByTrackId.Values.Should().Contain(true);
        streamingCoverage.ByTrackId.Values.Should().Contain(false);
    }
}
