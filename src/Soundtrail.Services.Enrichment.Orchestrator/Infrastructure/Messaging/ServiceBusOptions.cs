namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string CatalogSearchAttemptsQueueName { get; init; } = "lookup-music-requests";
}
