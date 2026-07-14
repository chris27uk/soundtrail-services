using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTracksForArtist;

public sealed class ArtistTracksExistTests
{
    public static TheoryData<GetTracksForArtistPortImplementation> Implementations => new()
    {
        GetTracksForArtistPortImplementation.Fake,
        GetTracksForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_Artist_Tracks_Are_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Id_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, artistId: "artist-2703");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistId.Should().Be(ArtistId.From("artist-2703"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Name_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, artistName: "Artist 2704");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be("Artist 2704");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Tracks_Are_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks.Should().HaveCount(1);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Id_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, trackId: "track-2803");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].TrackId.Should().Be(TrackId.From("track-2803"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Music_Catalog_Id_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, trackId: "track-2804");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].MusicCatalogId.Should().Be(new CatalogItemId.Track(TrackId.From("track-2804")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Title_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, title: "Track 2805");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].Title.Should().Be("Track 2805");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artist_Name_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, trackArtistName: "Artist 2806");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ArtistName.Should().Be("Artist 2806");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Album_Title_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, albumTitle: "Album 2807");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].AlbumTitle.Should().Be("Album 2807");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Duration_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, durationMs: 207000);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].DurationMs.Should().Be(207000);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Isrc_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, isrc: "GBAYE2402809");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].Isrc.Should().Be("GBAYE2402809");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Release_Date_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artwork_Url_Is_Returned(GetTracksForArtistPortImplementation implementation)
    {
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(implementation, artworkUrl: "https://cdn.soundtrail.test/tracks/track-2811.jpg");

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/track-2811.jpg");
    }
}
