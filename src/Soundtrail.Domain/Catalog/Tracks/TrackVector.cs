namespace Soundtrail.Domain.Catalog.Tracks;

public sealed record TrackVector(
    uint AlbumDiscriminator,
    int? ReleaseDateOrdinal,
    uint ReleaseTypeDiscriminator)
{
    public DateOnly? ReleaseDate =>
        ReleaseDateOrdinal is { } ordinal
            ? DateOnly.FromDayNumber(ordinal)
            : null;

    public bool HasAlbumDiscriminator => AlbumDiscriminator != 0;

    public bool HasReleaseTypeDiscriminator => ReleaseTypeDiscriminator != 0;
}
