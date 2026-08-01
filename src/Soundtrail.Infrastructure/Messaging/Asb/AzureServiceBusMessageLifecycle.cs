using Azure.Messaging.ServiceBus;

namespace Soundtrail.Adapters.Messaging;

internal sealed class AzureServiceBusMessageLifecycle(
    string queueName,
    ServiceBusReceivedMessage message,
    ProcessMessageEventArgs args,
    AzureServiceBusMessageTransport transport,
    int retryCount) : IMessageLifecycle
{
    public Task CompleteAsync(CancellationToken cancellationToken)
    {
        return args.CompleteMessageAsync(message, cancellationToken);
    }

    public async Task RetryAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        await transport.ScheduleRetryAsync(
            queueName,
            message,
            retryCount + 1,
            delay,
            cancellationToken);

        await args.CompleteMessageAsync(message, cancellationToken);
    }

    public Task DeadLetterAsync(string reason, string description, CancellationToken cancellationToken)
    {
        return args.DeadLetterMessageAsync(
            message,
            reason,
            description,
            cancellationToken);
    }
}
