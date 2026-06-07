namespace Soundtrail.Services.Enrichment.Responder.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string EnrichmentResponsesQueueName { get; init; } = string.Empty;
}
