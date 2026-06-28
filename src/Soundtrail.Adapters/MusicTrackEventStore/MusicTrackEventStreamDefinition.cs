using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;

namespace Soundtrail.Adapters.MusicTrackEventStore;

public static class MusicTrackEventStreamDefinition
{
    public static RavenEventStreamDefinition Create() =>
        new("music-track");
}
