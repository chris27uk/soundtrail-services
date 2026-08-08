using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Tests.Integration.Shared.Projector.StoreDiscoveryFeedback;

public sealed class StoreDiscoveryFeedbackPortContractTests
{
    public static TheoryData<StoreDiscoveryFeedbackPortImplementation> Implementations => new()
    {
        StoreDiscoveryFeedbackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Playlist_Target_Is_Completed_When_A_Later_Attempt_Fails_Then_Completed_Status_Is_Preserved(
        StoreDiscoveryFeedbackPortImplementation implementation)
    {
        await using var environment = StoreDiscoveryFeedbackPortContractTestEnvironment.Create(implementation);
        var target = environment.PlaylistTarget();

        await environment.Subject.StoreAsync(
            new WorkCompleted(
                target,
                LookupPriorityBand.High,
                "Lookup completed.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        await environment.Subject.StoreAsync(
            new WorkAttemptFailed(
                target,
                "Transient provider failure.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 1, TimeSpan.Zero)),
            CancellationToken.None);

        var record = await environment.LoadFeedbackAsync(target);

        record.Should().NotBeNull();
        record!.Status.Should().Be("completed");
        record.Reason.Should().Be("Lookup completed.");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Playlist_Tracks_Document_Exists_When_Playlist_Discovery_Completes_Then_Discovery_Is_Embedded(
        StoreDiscoveryFeedbackPortImplementation implementation)
    {
        await using var environment = StoreDiscoveryFeedbackPortContractTestEnvironment.Create(implementation);
        var playlistId = PlaylistId.FromPlaylistName("world_top_100").Value;
        var target = environment.PlaylistTarget("world_top_100");
        await environment.SeedPlaylistAsync(
            new CatalogPlaylistTracksRecordDto
            {
                Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
                PlaylistId = playlistId,
                TrackIds = [],
                Tracks = [],
                UpdatedAt = DateTimeOffset.UtcNow
            });

        await environment.Subject.StoreAsync(
            new WorkCompleted(
                target,
                LookupPriorityBand.High,
                "Lookup completed.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        var playlist = await environment.LoadPlaylistAsync(playlistId);

        playlist.Should().NotBeNull();
        playlist!.Discovery.Should().NotBeNull();
        playlist.Discovery!.Status.Should().Be("completed");
        playlist.Discovery.Reason.Should().Be("Lookup completed.");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Unplayable_Playlist_Track_When_Streaming_Discovery_Is_Missing_Then_Playlist_Discovery_Is_Scheduled_For_Catch_Up(
        StoreDiscoveryFeedbackPortImplementation implementation)
    {
        await using var environment = StoreDiscoveryFeedbackPortContractTestEnvironment.Create(implementation);
        var playlistId = PlaylistId.FromPlaylistName("world_top_100").Value;
        var trackId = TestTrackIds.Create("unplayable-1");
        var playlistTarget = environment.PlaylistTarget("world_top_100");
        await environment.SeedPlaylistAsync(
            new CatalogPlaylistTracksRecordDto
            {
                Id = CatalogPlaylistTracksRecordDto.GetDocumentId(playlistId),
                PlaylistId = playlistId,
                TrackIds = [trackId.Value],
                Tracks =
                [
                    new CatalogPlaylistTrackRecordDto
                    {
                        TrackId = trackId.Value,
                        MusicCatalogId = trackId.Value,
                        Title = "Static Hearts",
                        ArtistName = "Paper Tigers",
                        StreamingLocations = []
                    }
                ],
                UpdatedAt = DateTimeOffset.UtcNow
            });

        await environment.Subject.StoreAsync(
            new WorkCompleted(
                playlistTarget,
                LookupPriorityBand.High,
                "Lookup completed.",
                new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        var playlist = await environment.LoadPlaylistAsync(playlistId);

        playlist!.Discovery.Should().NotBeNull();
        playlist.Discovery!.Status.Should().Be("scheduled");
        playlist.Discovery.Reason.Should().Be("Track streaming projection is still catching up.");
    }
}
