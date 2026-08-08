namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;
}
