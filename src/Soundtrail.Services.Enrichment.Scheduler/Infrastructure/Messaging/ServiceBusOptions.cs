namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string LookupMusicRequestsQueueName { get; init; } = "lookup-music-requests";

    public string LookupMusicHighQueueName { get; init; } = "lookup-music-high";

    public string LookupMusicLowQueueName { get; init; } = "lookup-music-low";
}
