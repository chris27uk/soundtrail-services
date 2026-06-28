using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Adapters.Discovery;

public static class DiscoveryQueryEventStreamDefinition
{
    public static RavenEventStreamDefinition Create() =>
        new("discovery-query");
}
