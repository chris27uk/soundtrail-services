using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Soundtrail.Adapters.Messaging.Contracts;
using Soundtrail.Adapters.Projection;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging;

internal sealed class IncomingMessageSession<TDto, TDomain>(
    IMessageBodyDeserializer deserializer,
    IServiceScopeFactory scopeFactory,
    ExponentialRetryPolicy retryPolicy,
    ILogger<IncomingMessageSession<TDto, TDomain>> logger)
    where TDto : class
    where TDomain : class
{
    public async Task ProcessAsync(
        TransportEnvelope envelope,
        IMessageLifecycle lifecycle,
        CancellationToken cancellationToken)
    {
        using var activity = MessageTelemetry.StartHandleActivity(
            envelope,
            typeof(TDto),
            typeof(TDomain));
        ICommandBus? commandBus = null;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var typeRegistry = scope.ServiceProvider.GetRequiredService<ITypeRegistry>();
            var handlers = scope.ServiceProvider.GetRequiredService<HandlerCollection>();
            commandBus = scope.ServiceProvider.GetService<ICommandBus>();
            var dto = deserializer.Deserialize<TDto>(envelope.Body);
            var message = typeof(TDto) == typeof(TDomain)
                ? (TDomain)(object)dto
                : typeRegistry.ToDomainObject<TDomain>(dto);
            var incomingMessage = new IncomingMessage<TDomain>(
                message,
                new MessageMetadata(
                    envelope.Metadata.MessageId,
                    envelope.Metadata.CorrelationId,
                    envelope.Metadata.ReplyTo,
                    envelope.Metadata.QueueName,
                    envelope.Metadata.RetryCount,
                    envelope.Metadata.ApplicationProperties),
                commandBus is null ? null : ReplyAsync);

            if (message is IMessage domainMessage)
            {
                MessageTelemetry.EnrichCurrentActivity(domainMessage, "handle");
            }
            else
            {
                MessageTelemetry.SetDomainEventName(message.GetType());
            }

            MessageTelemetry.RecordHandleMessageEvent(activity);

            await handlers.HandleAsync(incomingMessage, cancellationToken);

            MessageTelemetry.AddCurrentEvent("message.processed");
            await lifecycle.CompleteAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Failed processing Azure Service Bus message for {DtoType} on {QueueName} (delivery {DeliveryCount}, retry {RetryCount}).",
                typeof(TDto).FullName,
                envelope.Metadata.QueueName,
                envelope.DeliveryCount,
                envelope.Metadata.RetryCount);

            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity.Current?.AddEvent(
                new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message }
                    }));

            var delay = retryPolicy.GetDelay(envelope.Metadata.RetryCount);
            if (delay is not null)
            {
                await lifecycle.RetryAsync(delay.Value, cancellationToken);

                MessageTelemetry.AddCurrentEvent("message.retried");
                Activity.Current?.SetTag("soundtrail.retry_count", envelope.Metadata.RetryCount + 1);
                Activity.Current?.SetTag("soundtrail.retry_delay_ms", delay.Value.TotalMilliseconds);
                return;
            }

            MessageTelemetry.AddCurrentEvent("message.dead_lettered");
            await lifecycle.DeadLetterAsync(
                "MessageProcessingFailed",
                Truncate(ex.ToString(), 4096),
                cancellationToken);
        }

        Task ReplyAsync(IMessage replyMessage, CancellationToken replyCancellationToken)
        {
            MessageTelemetry.AddCurrentEvent("message.replying");
            Activity.Current?.SetTag("soundtrail.reply.message_type", replyMessage.GetType().FullName);
            Activity.Current?.SetTag("soundtrail.reply.correlation_id", replyMessage.CorrelationId.Value);
            return commandBus!.SendAsync(replyMessage, replyCancellationToken);
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
