using Soundtrail.Adapters.EventSourcing;

namespace Soundtrail.Adapters.MusicTrackEventStore;

public static class ArtistCatalogEventStreamDefinition
{
    public static RavenEventStreamDefinition Create() => new("artist-catalog");
}
