using Soundtrail.Domain.Catalog.IntegrationEvents;

namespace Soundtrail.Domain.Catalog.Commands;

public sealed record PublishMusicTrackEventsCommand(
    IReadOnlyCollection<VersionedMusicTrackIntegrationEvent> Events);
