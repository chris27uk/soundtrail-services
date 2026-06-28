using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Adapters.Discovery;

public static class CatalogDiscoveryWorkEventStreamDefinition
{
    public static RavenEventStreamDefinition Create() =>
        new("catalog-discovery-work");
}
