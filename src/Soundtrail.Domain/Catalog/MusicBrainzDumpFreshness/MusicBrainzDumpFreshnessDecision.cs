using Soundtrail.Domain.Discovery;

namespace Soundtrail.Domain.Catalog.MusicBrainzDumpFreshness;

public sealed record MusicBrainzDumpFreshnessDecision(
    bool UseCatalog,
    IReadOnlyList<CatalogDiscoveryEntry> CatalogEntries)
{
    public static MusicBrainzDumpFreshnessDecision NeedsLiveLookup() =>
        new(false, []);

    public static MusicBrainzDumpFreshnessDecision UseExistingCatalog(
        IReadOnlyList<CatalogDiscoveryEntry> catalogEntries) =>
        new(true, catalogEntries);
}
