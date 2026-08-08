using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Integration.Features.GetTracksForArtist.Scenarios.Api.Ports;

public sealed class ArtistTracksExistTests
{
    public static TheoryData<GetTracksForArtistPortImplementation> Implementations => new()
    {
        GetTracksForArtistPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_Artist_Tracks_Are_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var artistId = "artist-2701";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Id_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var artistId = "artist-2703";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistId.Should().Be(ArtistId.From(artistId));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Artist_Name_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var artistName = "Artist 2704";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            artistName: artistName);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.ArtistName.Value.Should().Be(artistName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Tracks_Are_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var trackCount = 1;
        var artistId = "artist-2701";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            artistId: artistId);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks.Should().HaveCount(trackCount);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Id_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var trackIdValue = TestTrackIds.Value("track-2803");
        var expectedTrackId = TestTrackIds.Create("track-2803");
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            trackId: trackIdValue);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].TrackId.Should().Be(expectedTrackId);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Title_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var title = "Track 2805";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            title: title);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].Title.Should().Be(title);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artist_Name_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var trackArtistName = "Artist 2806";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            trackArtistName: trackArtistName);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ArtistName.Should().Be(trackArtistName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Album_Title_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var albumTitle = "Album 2807";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            albumTitle: albumTitle);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].AlbumTitle.Should().Be(albumTitle);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Duration_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var durationMs = 207000;
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            durationMs: durationMs);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].DurationMs.Should().Be(durationMs);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Isrc_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var isrc = "GBAYE2402809";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            isrc: isrc);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].Isrc.Should().Be(isrc);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Release_Date_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            releaseDate: releaseDate);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_Existing_Artist_Tracks_When_Requesting_The_Artist_Tracks_Then_The_Track_Artwork_Url_Is_Returned(
        GetTracksForArtistPortImplementation implementation)
    {
        var artworkUrl = "https://cdn.soundtrail.test/tracks/track-2811.jpg";
        await using var environment = await GetTracksForArtistPortContractTestEnvironment.ForExistingArtistTracks(
            implementation,
            artworkUrl: artworkUrl);

        var result = await environment.Subject.GetTracksForArtistAsync(environment.ArtistId, CancellationToken.None);

        result!.Tracks[0].ArtworkUrl.Should().Be(artworkUrl);
    }
}
