namespace Soundtrail.Domain.Catalog;

public sealed record ArtworkReference(
    Uri Url,
    string Source,
    DateTimeOffset DiscoveredAt);
