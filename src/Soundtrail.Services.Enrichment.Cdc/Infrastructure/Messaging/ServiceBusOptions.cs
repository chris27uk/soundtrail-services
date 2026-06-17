namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string MusicTrackEventsQueueName { get; init; } = string.Empty;
}
