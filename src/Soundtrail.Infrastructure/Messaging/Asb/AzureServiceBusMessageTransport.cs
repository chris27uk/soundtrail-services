using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Soundtrail.Adapters.Messaging;

internal sealed class AzureServiceBusMessageTransport(
    AzureServiceBusMessageProcessingOptions options,
    JsonSerializerOptions serializerOptions,
    ILogger<AzureServiceBusMessageTransport> logger) : IAsyncDisposable
{
    private readonly Lazy<ServiceBusClient> client = new(() => new ServiceBusClient(options.ConnectionString));
    private readonly ConcurrentDictionary<string, ServiceBusSender> senders = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEnabled => options.Enabled;

    public ServiceBusProcessor CreateProcessor(string queueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return client.Value.CreateProcessor(
            queueName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 1
            });
    }

    public async Task SendAsync(
        string queueName,
        object message,
        ServiceBusMessage envelope,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(envelope);

        if (!IsEnabled)
        {
            throw new InvalidOperationException(
                $"Azure Service Bus transport is disabled. Unable to send '{message.GetType().FullName}' to '{queueName}'.");
        }

        var sender = GetSender(queueName);
        envelope.Body = new BinaryData(JsonSerializer.SerializeToUtf8Bytes(message, serializerOptions));
        envelope.ContentType = "application/json";
        envelope.Subject ??= message.GetType().FullName;

        await sender.SendMessageAsync(envelope, cancellationToken);
    }

    public TMessage Deserialize<TMessage>(ServiceBusReceivedMessage message)
    {
        var deserialized = JsonSerializer.Deserialize<TMessage>(message.Body, serializerOptions);
        if (deserialized is null)
        {
            throw new InvalidOperationException(
                $"The message body for '{typeof(TMessage).FullName}' could not be deserialized.");
        }

        return deserialized;
    }

    public async Task ScheduleRetryAsync(
        string queueName,
        ServiceBusReceivedMessage message,
        int retryCount,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        var sender = GetSender(queueName);
        var retryMessage = CloneMessage(message, retryCount);
        var scheduledEnqueueTime = DateTimeOffset.UtcNow.Add(delay);

        await sender.ScheduleMessageAsync(retryMessage, scheduledEnqueueTime, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in senders.Values)
        {
            await sender.DisposeAsync();
        }

        if (client.IsValueCreated)
        {
            await client.Value.DisposeAsync();
        }
    }

    private ServiceBusSender GetSender(string queueName)
    {
        return senders.GetOrAdd(
            queueName,
            name =>
            {
                logger.LogDebug("Creating Azure Service Bus sender for queue {QueueName}", name);
                return client.Value.CreateSender(name);
            });
    }

    private static ServiceBusMessage CloneMessage(ServiceBusReceivedMessage message, int retryCount)
    {
        var retryMessage = new ServiceBusMessage(message.Body)
        {
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId,
            Subject = message.Subject,
            ReplyTo = message.ReplyTo,
            ReplyToSessionId = message.ReplyToSessionId,
            SessionId = message.SessionId,
            To = message.To,
            MessageId = Guid.NewGuid().ToString("N")
        };

        foreach (var property in message.ApplicationProperties)
        {
            retryMessage.ApplicationProperties[property.Key] = property.Value;
        }

        retryMessage.ApplicationProperties["soundtrail-retry-count"] = retryCount;
        retryMessage.ApplicationProperties["soundtrail-original-message-id"] = message.MessageId;

        return retryMessage;
    }
}
