using Azure.Messaging.ServiceBus;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging.Asb;

internal sealed class AzureServiceBusCommandBus(
    AzureServiceBusMessageTransport transport) : ICommandBus
{
    public async Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var dto = TypeTranslationRegistry.Default.ToDto(message);
        var queueName = ServiceBusQueues.For(dto.GetType());

        using var activity = MessageTelemetry.StartPublishActivity(message, dto);
        await transport.SendAsync(
            queueName,
            dto,
            new ServiceBusMessage
            {
                MessageId = message.Id.Value,
                CorrelationId = message.CorrelationId.Value
            },
            cancellationToken);
    }
}
