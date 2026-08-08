using Soundtrail.Services.Tests.Integration.Features.GetTrack;
namespace Soundtrail.Services.Tests.Integration.Features.GetTrack.Scenarios.Api.Ports;

public sealed class TrackExistsTests
{
    public static TheoryData<GetTrackPortImplementation> Implementations => new()
    {
        GetTrackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_A_Track_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var trackId = TestTrackIds.Value("track-601");
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            trackId: trackId);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Track_Id_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var trackIdValue = TestTrackIds.Value("track-603");
        var expectedTrackId = TestTrackIds.Create("track-603");
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            trackId: trackIdValue);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.TrackId.Should().Be(expectedTrackId);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Title_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var title = "Track 604";
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            title: title);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.Title.Should().Be(title);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artist_Name_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var artistName = "Artist 605";
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            artistName: artistName);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ArtistName.Should().Be(artistName);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Album_Title_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var albumTitle = "Album 606";
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            albumTitle: albumTitle);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.AlbumTitle.Should().Be(albumTitle);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Duration_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var durationMs = 207000;
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            durationMs: durationMs);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.DurationMs.Should().Be(durationMs);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Isrc_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var isrc = "GBAYE2400607";
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            isrc: isrc);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.Isrc.Should().Be(isrc);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Release_Date_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            releaseDate: releaseDate);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artwork_Url_Is_Returned(
        GetTrackPortImplementation implementation)
    {
        var artworkUrl = "https://cdn.soundtrail.test/tracks/mc_track_608.jpg";
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(
            implementation,
            artworkUrl: artworkUrl);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ArtworkUrl.Should().Be(artworkUrl);
    }
}
