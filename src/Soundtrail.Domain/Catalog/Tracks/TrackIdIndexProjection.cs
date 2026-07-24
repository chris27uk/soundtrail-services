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
}
