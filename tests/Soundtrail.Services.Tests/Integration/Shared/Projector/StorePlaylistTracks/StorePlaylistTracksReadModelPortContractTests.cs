using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Discovery.Events;

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
        var discoveredTrackId = TestTrackIds.Create("spotify-track");
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

        var parentTrackId = MustCreate("Neon Harbour", "Glass Cities");
        var childTrackId = MustCreate(
            "Neon Harbour",
            "Glass Cities (Radio Edit)",
            "Glass Cities Remixes",
            new DateOnly(2024, 6, 23),
            "Radio Edit");

        await environment.SeedCatalogTrackAsync(
            CreateCatalogTrack(
                childTrackId,
                title: "Glass Cities (Radio Edit)",
                artistName: "Neon Harbour",
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

        var parentTrackId = MustCreate("Neon Harbour", "Glass Cities");
        var childTrackId = MustCreate(
            "Neon Harbour",
            "Glass Cities (Radio Edit)",
            "Glass Cities Remixes",
            new DateOnly(2024, 6, 23),
            "Radio Edit");

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
                artistName: "Neon Harbour",
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

    private static CatalogTrackRecordDto CreateCatalogTrack(
        TrackId trackId,
        string title,
        string artistName,
        string? albumTitle = "Album",
        DateOnly? releaseDate = null,
        string? releaseType = "studio") =>
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
