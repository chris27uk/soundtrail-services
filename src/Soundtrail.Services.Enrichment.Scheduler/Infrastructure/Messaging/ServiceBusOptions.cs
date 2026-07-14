namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string DiscoveryBacklogSchedulingQueueName { get; init; } = "discovery-backlog-scheduling";

    public string PlaylistUpdatesQueueName { get; init; } = "playlist-updates";
}
