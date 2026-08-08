namespace Soundtrail.Adapters.Messaging;

internal sealed class AzureServiceBusMessageProcessingOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public bool Enabled { get; init; }
}
