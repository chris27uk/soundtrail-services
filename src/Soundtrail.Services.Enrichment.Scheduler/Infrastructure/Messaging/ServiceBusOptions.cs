namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string LookupMusicRequestsQueueName { get; init; } = "lookup-music-requests";

    public string HighPriorityMusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz-high";

    public string LowPriorityMusicBrainzLookupQueueName { get; init; } = "lookup-musicbrainz-low";

    public string HighPriorityAppleLookupQueueName { get; init; } = "lookup-apple-high";

    public string LowPriorityAppleLookupQueueName { get; init; } = "lookup-apple-low";

    public string HighPriorityYouTubeMusicLookupQueueName { get; init; } = "lookup-youtubemusic-high";

    public string LowPriorityYouTubeMusicLookupQueueName { get; init; } = "lookup-youtubemusic-low";
}
