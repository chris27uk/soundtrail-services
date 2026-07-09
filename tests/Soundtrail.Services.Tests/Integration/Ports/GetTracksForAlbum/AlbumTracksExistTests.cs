using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForAlbum;

public sealed class AlbumTracksExistTests
{
    public static TheoryData<GetTracksForAlbumPortImplementation> Implementations => new()
    {
        GetTracksForAlbumPortImplementation.Fake,
        GetTracksForAlbumPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_Album_Tracks_Are_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation);

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Artist_Id_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, artistId: "artist-1103");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.ArtistId.Value.Should().Be("artist-1103");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Album_Id_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, artistId: "artist-1104", albumId: "album-1204");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumId.Should().Be(AlbumId.From("artist-1104", "album-1204"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Album_Title_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, albumTitle: "Album 1205");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.AlbumTitle.Should().Be("Album 1205");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Tracks_Are_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation);

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks.Should().HaveCount(1);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Id_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, trackId: "track-1303");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].TrackId.Should().Be(TrackId.From("track-1303"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Music_Catalog_Id_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, trackId: "track-1304");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].MusicCatalogId.Should().Be(new MusicCatalogId.Track(TrackId.From("track-1304")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Title_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, title: "Track 1305");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].Title.Should().Be("Track 1305");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Artist_Name_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, artistName: "Artist 1306");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].ArtistName.Should().Be("Artist 1306");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Duration_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, durationMs: 207000);

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].DurationMs.Should().Be(207000);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Isrc_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, isrc: "GBAYE2401308");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].Isrc.Should().Be("GBAYE2401308");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Release_Date_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Album_Tracks_When_Requesting_The_Album_Tracks_Then_The_Track_Artwork_Url_Is_Returned(GetTracksForAlbumPortImplementation implementation)
    {
        await using var environment = await GetTracksForAlbumPortContractTestEnvironment.ForExistingAlbumTracks(implementation, artworkUrl: "https://cdn.soundtrail.test/tracks/track-1310.jpg");

        var result = await environment.Subject.GetTracksForAlbumAsync(environment.AlbumId, CancellationToken.None);

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-1310.jpg");
    }
}
