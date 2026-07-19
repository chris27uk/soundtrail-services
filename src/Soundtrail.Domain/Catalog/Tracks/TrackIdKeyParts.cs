namespace Soundtrail.Domain.Catalog.Tracks;

public sealed record TrackIdKeyParts(
    string BaseKeyHigh,
    string BaseKeyLow,
    string SpecificKey);
