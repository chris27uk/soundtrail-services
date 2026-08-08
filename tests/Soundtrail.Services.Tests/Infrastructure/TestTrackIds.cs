using Soundtrail.Domain.Catalog.Tracks;

namespace Soundtrail.Services.Tests;

public static class TestTrackIds
{
    public static TrackId Create(string seed) =>
        ExpectSuccess(TrackId.TryCreate(
            artistName: "test artist",
            trackName: seed,
            albumName: "test album",
            releaseDate: new DateOnly(2000, 1, 1),
            releaseType: "studio"));

    public static string Value(string seed) => Create(seed).Value;

    private static TrackId ExpectSuccess(TrackIdCreateResult result) =>
        result switch
        {
            TrackIdCreateResult.Success success => success.Value,
            TrackIdCreateResult.Failure failure => throw new InvalidOperationException(failure.Reason),
            _ => throw new InvalidOperationException("Unexpected TrackId creation result.")
        };
}
