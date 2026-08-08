namespace Soundtrail.Adapters.Messaging.Asb;

internal sealed class AzureServiceBusMessageProcessingOptions
{
    public string ConnectionString { get; init; } = string.Empty;

    public bool Enabled { get; init; }
}
