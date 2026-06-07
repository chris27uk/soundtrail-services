namespace Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string MusicTrackEventsQueueName { get; init; } = "music-track-events";

    public string AppleLookupQueueName { get; init; } = "lookup-apple";

    public string YouTubeMusicLookupQueueName { get; init; } = "lookup-youtubemusic";
}
