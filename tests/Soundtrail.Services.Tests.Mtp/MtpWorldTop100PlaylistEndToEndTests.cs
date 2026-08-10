using System.Net.Http.Json;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

namespace Soundtrail.Services.Tests.Mtp;

[Collection(nameof(MtpEndToEndHostCollection))]
public sealed class MtpWorldTop100PlaylistEndToEndTests(MtpEndToEndHostFixture fixture)
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    [Fact]
    public async Task Given_An_Empty_Catalog_When_Requesting_WorldTop100_Then_Discovery_Completes_With_Fuzzy_Matched_Tracks()
    {
        MtpSpikeDiagnostics.RecordTestStart(nameof(MtpWorldTop100PlaylistEndToEndTests));

        var pending = await GetPlaylistAsync();

        pending.Should().NotBeNull();
        pending!.PlaylistId.Should().Be("worldtop100");
        pending.Tracks.Should().BeEmpty();
        pending.Discovery.Should().NotBeNull();
        pending.Discovery!.Status.Should().Be("scheduled");

        var resolved = await WaitForResolvedPlaylistAsync();

        resolved.Should().NotBeNull();
        resolved!.Tracks.Should().HaveCount(4);
        resolved.Discovery.Should().NotBeNull();
        resolved.Discovery!.Status.Should().Be("completed");
    }

    private async Task<GetTracksForPlaylistResponseDto?> GetPlaylistAsync()
    {
        var response = await fixture.ApiClient.GetAsync("/catalog/playlists/world_top_100/tracks");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();
    }

    private async Task<GetTracksForPlaylistResponseDto?> WaitForResolvedPlaylistAsync()
    {
        var deadline = DateTime.UtcNow.Add(PollTimeout);
        GetTracksForPlaylistResponseDto? latest = null;

        while (DateTime.UtcNow < deadline)
        {
            latest = await GetPlaylistAsync();
            if (IsFullyResolved(latest))
            {
                return latest;
            }

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException($"World Top 100 playlist did not resolve within {PollTimeout}.");
    }

    private static bool IsFullyResolved(GetTracksForPlaylistResponseDto? response) =>
        response is
        {
            Discovery.Status: "completed",
            Tracks.Length: 4
        }
        && response.Tracks.Count(static track => track.Playable) == 2;

}
