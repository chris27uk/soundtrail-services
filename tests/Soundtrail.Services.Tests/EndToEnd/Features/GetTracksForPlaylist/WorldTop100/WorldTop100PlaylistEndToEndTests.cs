using System.Net.Http.Json;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForPlaylist.Adapters;

namespace Soundtrail.Services.Tests.EndToEnd.Features.GetTracksForPlaylist.WorldTop100;

[Collection(nameof(EndToEndHostCollection))]
public sealed class WorldTop100PlaylistEndToEndTests(EndToEndHostFixture fixture)
{
    // HTTP GET also publishes RequestKnownMusicData; polling the API floods the bus on 2-vCPU CI.
    // Wait on the Raven read model, then one GET to assert the HTTP contract.
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly string PlaylistDocumentId =
        CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.FromPlaylistName("world_top_100").Value);

    [Fact]
    public async Task Given_An_Empty_Catalog_When_Requesting_WorldTop100_Then_Discovery_Completes_With_Fuzzy_Matched_Tracks()
    {
        var pending = await GetPlaylistAsync();

        pending.Should().NotBeNull();
        pending!.PlaylistId.Should().Be("worldtop100");
        pending.Tracks.Should().BeEmpty();
        pending.Discovery.Should().NotBeNull();
        pending.Discovery!.Status.Should().Be("scheduled");

        var resolved = await WaitForResolvedPlaylistAsync();

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
    }

    private async Task<GetTracksForPlaylistResponseDto?> GetPlaylistAsync()
    {
        var response = await fixture.ApiClient.GetAsync("/catalog/playlists/world_top_100/tracks");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetTracksForPlaylistResponseDto>();
    }

    private async Task<GetTracksForPlaylistResponseDto?> WaitForResolvedPlaylistAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(PollTimeout);

        try
        {
            while (!timeout.Token.IsCancellationRequested)
            {
                if (PlaylistReadModelIsResolved())
                {
                    return await GetPlaylistAsync();
                }

                await Task.Delay(PollInterval, timeout.Token);
            }
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
        }

        throw new TimeoutException(FormatTimeout(await GetPlaylistAsync()));
    }

    private bool PlaylistReadModelIsResolved()
    {
        using var session = fixture.DocumentStore.OpenSession();
        var record = session.Load<CatalogPlaylistTracksRecordDto>(PlaylistDocumentId);
        if (record is not { Discovery.Status: "completed", Tracks.Length: 4 })
        {
            return false;
        }

        return record.Tracks.Count(static track => track.StreamingLocations.Length > 0) == 2
            && record.Tracks
                .SelectMany(static track => track.StreamingLocations)
                .Select(static location => location.Url)
                .OrderBy(static url => url, StringComparer.Ordinal)
                .SequenceEqual(
                [
                    "https://music.youtube.com/watch?v=glass-cities-radio",
                    "https://open.spotify.com/track/midnight-signals"
                ]);
    }

    private static string FormatTimeout(GetTracksForPlaylistResponseDto? latest)
    {
        if (latest is null)
        {
            return $"World Top 100 playlist did not resolve within {PollTimeout}. Latest response was null.";
        }

        var tracks = string.Join(
            " | ",
            latest.Tracks.Select(track =>
                $"{track.Title}/playable={track.Playable}/urls=[{string.Join(", ", track.StreamingLocations.Select(location => location.Url))}]"));

        return $"World Top 100 playlist did not resolve within {PollTimeout}. " +
               $"track count={latest.Tracks.Length}, " +
               $"playable count={latest.Tracks.Count(track => track.Playable)}, " +
               $"discovery status={latest.Discovery?.Status ?? "<null>"}, " +
               $"discovery reason={latest.Discovery?.Reason ?? "<null>"}, " +
               $"tracks=[{tracks}]. " +
               "If status=completed with missing playable URLs, streaming WorkCompleted likely raced ahead of playlist Repair.";
    }
}
