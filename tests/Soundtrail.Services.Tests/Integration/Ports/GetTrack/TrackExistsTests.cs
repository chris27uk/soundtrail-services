using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests.Integration.Ports.GetTrack;

public sealed class TrackExistsTests
{
    public static TheoryData<GetTrackPortImplementation> Implementations => new()
    {
        GetTrackPortImplementation.Fake,
        GetTrackPortImplementation.Raven
    };

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_A_Track_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Track_Id_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, trackId: "track-603");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.TrackId.Should().Be(TrackId.From("track-603"));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Music_Catalog_Id_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, trackId: "track-603");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.MusicCatalogId.Should().Be(new CatalogItemId.Track(TrackId.From("track-603")));
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Title_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, title: "Track 604");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.Title.Should().Be("Track 604");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artist_Name_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, artistName: "Artist 605");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ArtistName.Should().Be("Artist 605");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Album_Title_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, albumTitle: "Album 606");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.AlbumTitle.Should().Be("Album 606");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Duration_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, durationMs: 207000);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.DurationMs.Should().Be(207000);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Isrc_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, isrc: "GBAYE2400607");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.Isrc.Should().Be("GBAYE2400607");
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Release_Date_Is_Returned(GetTrackPortImplementation implementation)
    {
        var releaseDate = new DateOnly(2024, 11, 12);
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, releaseDate: releaseDate);

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ReleaseDate.Should().Be(releaseDate);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public async Task Given_An_Existing_Track_When_Requesting_The_Track_Then_The_Artwork_Url_Is_Returned(GetTrackPortImplementation implementation)
    {
        await using var environment = await GetTrackPortContractTestEnvironment.ForExistingTrack(implementation, artworkUrl: "https://cdn.soundtrail.test/tracks/mc_track_608.jpg");

        var result = await environment.Subject.GetTrackAsync(environment.TrackId, CancellationToken.None);

        result!.ArtworkUrl.Should().Be("https://cdn.soundtrail.test/tracks/mc_track_608.jpg");
    }
}
