namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string CatalogSearchAttemptsQueueName { get; init; } = "lookup-music-requests";

    public string KnownCatalogItemRequestsQueueName { get; init; } = "lookup-music-requests";
}
