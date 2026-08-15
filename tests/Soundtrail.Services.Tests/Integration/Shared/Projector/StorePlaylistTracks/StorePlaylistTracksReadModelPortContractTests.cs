using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Shared.Projector.StorePlaylistTracks;

public sealed class StorePlaylistTracksReadModelPortContractTests
{
    public static TheoryData<StorePlaylistTracksReadModelPortImplementation> Implementations => new()
    {
        StorePlaylistTracksReadModelPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Subsequent_Empty_Discovery_When_Storing_Then_Existing_Playlist_Tracks_Are_Preserved(
        StorePlaylistTracksReadModelPortImplementation implementation)
    {
        await using var environment = StorePlaylistTracksReadModelPortContractTestEnvironment.Create(implementation);
        var discoveredTrackId = TestTrackIds.Create(
            $"spotify-track-{EmbeddedRavenTestServer.NewIsolationKey()}");
        await environment.SeedCatalogTrackAsync(CreateCatalogTrack(discoveredTrackId, "Spotify Track", "Spotify Artist"));

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [discoveredTrackId],
                DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [],
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var record = await environment.LoadPlaylistTracksAsync();

        record.Should().NotBeNull();
        record!.TrackIds.Should().ContainSingle().Which.Should().Be(discoveredTrackId.Value);
        record.Tracks.Should().ContainSingle();
        record.Tracks[0].TrackId.Should().Be(discoveredTrackId.Value);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Parent_Playlist_Track_Id_When_Child_Catalog_Track_Exists_Then_Preferred_Child_Is_Stored(
        StorePlaylistTracksReadModelPortImplementation implementation)
    {
        await using var environment = StorePlaylistTracksReadModelPortContractTestEnvironment.Create(
            implementation,
            playlistName: "parent_child_playlist");

        var (artistName, parentTrackId, childTrackId) = CreateIsolatedParentChildPair();

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: artistName,
                albumTitle: "Glass Cities Remixes",
                releaseDate: new DateOnly(2024, 6, 23),
                releaseType: "Radio Edit"));

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [parentTrackId],
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var record = await environment.LoadPlaylistTracksAsync();

        record.Should().NotBeNull();
        record!.TrackIds.Should().ContainSingle().Which.Should().Be(parentTrackId.Value);
        record.Tracks.Should().ContainSingle();
        record.Tracks[0].TrackId.Should().Be(childTrackId.Value);
        record.Tracks[0].Title.Should().Be("Glass Cities (Radio Edit)");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Playlist_References_Parent_Track_When_Child_Catalog_Track_Arrives_Then_Repair_Updates_Preferred_Child(
        StorePlaylistTracksReadModelPortImplementation implementation)
    {
        await using var environment = StorePlaylistTracksReadModelPortContractTestEnvironment.Create(
            implementation,
            playlistName: "repair_playlist");

        var (artistName, parentTrackId, childTrackId) = CreateIsolatedParentChildPair();

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [parentTrackId],
                DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: artistName,
                albumTitle: "Glass Cities Remixes",
                releaseDate: new DateOnly(2024, 6, 23),
                releaseType: "Radio Edit"));

        await environment.Subject.RepairTrackAsync(childTrackId, CancellationToken.None);

        var record = await environment.LoadPlaylistTracksAsync();

        record.Should().NotBeNull();
        record!.Tracks.Should().ContainSingle();
        record.Tracks[0].TrackId.Should().Be(childTrackId.Value);
        record.Tracks[0].Title.Should().Be("Glass Cities (Radio Edit)");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Parent_Without_Streaming_And_Child_With_Streaming_When_Storing_Then_Child_Streaming_Locations_Are_Preferred(
        StorePlaylistTracksReadModelPortImplementation implementation)
    {
        await using var environment = StorePlaylistTracksReadModelPortContractTestEnvironment.Create(
            implementation,
            playlistName: "streaming_preference_store");

        var (artistName, parentTrackId, childTrackId) = CreateIsolatedParentChildPair();

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                parentTrackId,
                title: "Glass Cities",
                artistName: artistName));
        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: artistName,
                albumTitle: "Glass Cities Remixes",
                releaseDate: new DateOnly(2024, 6, 23),
                releaseType: "Radio Edit",
                streamingUrl: "https://music.youtube.com/watch?v=glass-cities-radio"));

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [parentTrackId],
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var record = await environment.LoadPlaylistTracksAsync();

        record.Should().NotBeNull();
        record!.Tracks.Should().ContainSingle();
        record.Tracks[0].TrackId.Should().Be(childTrackId.Value);
        record.Tracks[0].StreamingLocations.Should().ContainSingle()
            .Which.Url.Should().Be("https://music.youtube.com/watch?v=glass-cities-radio");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Playlist_Stuck_On_Parent_Without_Streaming_When_Child_Streaming_Arrives_Then_Repair_Promotes_Playable_Child(
        StorePlaylistTracksReadModelPortImplementation implementation)
    {
        await using var environment = StorePlaylistTracksReadModelPortContractTestEnvironment.Create(
            implementation,
            playlistName: "streaming_preference_repair");

        var (artistName, parentTrackId, childTrackId) = CreateIsolatedParentChildPair();

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                parentTrackId,
                title: "Glass Cities",
                artistName: artistName));
        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: artistName,
                albumTitle: "Glass Cities Remixes",
                releaseDate: new DateOnly(2024, 6, 23),
                releaseType: "Radio Edit"));

        await environment.Subject.StoreAsync(
            new PlaylistTracksDiscovered(
                environment.PlaylistId,
                [parentTrackId],
                DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        var before = await environment.LoadPlaylistTracksAsync();
        before.Should().NotBeNull();
        before!.Tracks[0].StreamingLocations.Should().BeEmpty();

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: artistName,
                albumTitle: "Glass Cities Remixes",
                releaseDate: new DateOnly(2024, 6, 23),
                releaseType: "Radio Edit",
                streamingUrl: "https://music.youtube.com/watch?v=glass-cities-radio"));

        await environment.Subject.RepairTrackAsync(childTrackId, CancellationToken.None);

        var after = await environment.LoadPlaylistTracksAsync();
        after.Should().NotBeNull();
        after!.Tracks.Should().ContainSingle();
        after.Tracks[0].TrackId.Should().Be(childTrackId.Value);
        after.Tracks[0].StreamingLocations.Should().ContainSingle()
            .Which.Url.Should().Be("https://music.youtube.com/watch?v=glass-cities-radio");
    }

    private static (string ArtistName, TrackId Parent, TrackId Child) CreateIsolatedParentChildPair()
    {
        var isolation = EmbeddedRavenTestServer.NewIsolationKey();
        var artistName = $"Neon Harbour {isolation}";
        var parentTrackId = MustCreate(artistName, "Glass Cities");
        var childTrackId = MustCreate(
            artistName,
            "Glass Cities (Radio Edit)",
            "Glass Cities Remixes",
            new DateOnly(2024, 6, 23),
            "Radio Edit");
        return (artistName, parentTrackId, childTrackId);
    }

    private static CatalogTrackRecordDto CreateCatalogTrack(
        TrackId trackId,
        string title,
        string artistName,
        string? albumTitle = "Album",
        DateOnly? releaseDate = null,
        string? releaseType = "studio",
        string? streamingUrl = null) =>
        new()
        {
            Id = CatalogTrackRecordDto.GetDocumentId(trackId.Value),
            TrackId = trackId.Value,
            MusicCatalogId = trackId.Value,
            Title = title,
            ArtistName = artistName,
            AlbumTitle = albumTitle,
            DurationMs = 180000,
            ReleaseDate = releaseDate ?? new DateOnly(2024, 1, 1),
            ReleaseType = releaseType,
            StreamingLocations = streamingUrl is null
                ? []
                :
                [
                    new CatalogStreamingLocationRecordDto
                    {
                        Provider = "youtubeMusic",
                        Url = streamingUrl
                    }
                ],
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static TrackId MustCreate(
        string artistName,
        string trackName,
        string? albumName = null,
        DateOnly? releaseDate = null,
        string? releaseType = null) =>
        TrackId.TryCreate(artistName, trackName, albumName, releaseDate, releaseType) switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unsupported track id creation result.")
        };
}
