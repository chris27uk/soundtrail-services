using System.Globalization;

namespace Soundtrail.Domain.Catalog.Tracks;

public sealed record TrackIdIndexProjection(
    ulong BaseHigh,
    ulong BaseLow,
    uint AlbumDiscriminator,
    uint ReleaseDateOrdinal,
    uint ReleaseTypeDiscriminator)
{
    public static TrackIdIndexProjection From(TrackId trackId)
    {
        var baseSpan = trackId.BaseComponent.AsSpan();
        var baseHigh = ulong.Parse(baseSpan[..16], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        var baseLow = ulong.Parse(baseSpan[16..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return new TrackIdIndexProjection(
            baseHigh,
            baseLow,
            trackId.Vector.AlbumDiscriminator,
            (uint)(trackId.Vector.ReleaseDateOrdinal ?? 0),
            trackId.Vector.ReleaseTypeDiscriminator);
    }

    public bool SharesBaseWith(TrackIdIndexProjection other) =>
        BaseHigh == other.BaseHigh && BaseLow == other.BaseLow;

    public long GetDistanceTo(TrackIdIndexProjection target)
    {
        const long albumMismatchPenalty = 1_000_000_000_000L;
        const long albumMissingPenalty = 250_000_000_000L;
        const long albumSpecificityPenalty = 10_000_000L;
        const long releaseTypeMismatchPenalty = 100_000_000_000L;
        const long releaseTypeMissingPenalty = 25_000_000_000L;
        const long releaseTypeSpecificityPenalty = 1_000_000L;
        const long releaseDateMissingPenalty = 5_000_000_000L;
        const long releaseDateSpecificityPenalty = 100_000L;

        long distance = 0;

        distance += CompareOptionalDiscriminator(
            candidate: AlbumDiscriminator,
            target: target.AlbumDiscriminator,
            mismatchPenalty: albumMismatchPenalty,
            missingPenalty: albumMissingPenalty,
            specificityPenalty: albumSpecificityPenalty);

        distance += CompareOptionalDiscriminator(
            candidate: ReleaseTypeDiscriminator,
            target: target.ReleaseTypeDiscriminator,
            mismatchPenalty: releaseTypeMismatchPenalty,
            missingPenalty: releaseTypeMissingPenalty,
            specificityPenalty: releaseTypeSpecificityPenalty);

        if (target.ReleaseDateOrdinal != 0)
        {
            distance += ReleaseDateOrdinal == 0
                ? releaseDateMissingPenalty
                : Math.Abs((long)ReleaseDateOrdinal - target.ReleaseDateOrdinal);
        }
        else if (ReleaseDateOrdinal != 0)
        {
            distance += releaseDateSpecificityPenalty;
        }

        return distance;
    }

    private static long CompareOptionalDiscriminator(
        uint candidate,
        uint target,
        long mismatchPenalty,
        long missingPenalty,
        long specificityPenalty)
    {
        if (target == 0)
        {
            return candidate == 0 ? 0 : specificityPenalty;
        }

        if (candidate == 0)
        {
            return missingPenalty;
        }

        return candidate == target ? 0 : mismatchPenalty;
    }
}
