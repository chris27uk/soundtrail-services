using Soundtrail.Adapters.EventSourcing;

namespace Soundtrail.Adapters.Enrichment;

public static class MusicCatalogLookupEventStreamDefinition
{
    public static RavenEventStreamDefinition Create() => new("music-catalog-lookup");
}
