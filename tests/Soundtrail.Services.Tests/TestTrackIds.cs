using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests;

public static class TestTrackIds
{
    public static TrackId Create(string seed) =>
        TrackId.Create(
            artistName: "test artist",
            trackName: seed,
            albumName: "test album",
            releaseDate: new DateOnly(2000, 1, 1),
            releaseType: "studio");

    public static string Value(string seed) => Create(seed).Value;
}
