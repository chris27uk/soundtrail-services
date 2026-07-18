namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string KnownMusicDataRequestsQueueName { get; init; } = "known-music-data-requests";

    public string UnknownMusicDataRequestsQueueName { get; init; } = "unknown-music-data-requests";

    public string AssessMusicCatalogItemQueueName { get; init; } = "assess-music-catalog-item";
}
