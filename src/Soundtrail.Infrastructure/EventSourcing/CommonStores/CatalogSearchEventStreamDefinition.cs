using Soundtrail.Adapters.EventSourcing;

namespace Soundtrail.Adapters.EventSourcing.CommonStores;

internal static class CatalogSearchEventStreamDefinition
{
    internal static RavenEventStreamDefinition Create() => new("catalog-search");
}
