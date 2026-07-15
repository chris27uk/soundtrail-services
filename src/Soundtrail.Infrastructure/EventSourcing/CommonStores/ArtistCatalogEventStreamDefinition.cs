using Soundtrail.Adapters.EventSourcing;

namespace Soundtrail.Adapters.MusicTrackEventStore;

internal static class ArtistCatalogEventStreamDefinition
{
    internal static RavenEventStreamDefinition Create() => new("artist-catalog");
}
