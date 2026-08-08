using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Adapters.Messaging.Asb;

internal sealed class AzureServiceBusMessageListenerHostedService<TDto, TDomain>(
    string queueName,
    AzureServiceBusMessageTransport transport,
    IncomingMessageSession<TDto, TDomain> session,
    IHostEnvironment environment,
    ILogger<AzureServiceBusMessageListenerHostedService<TDto, TDomain>> logger) : IHostedService, IAsyncDisposable
    where TDto : class
    where TDomain : class
{
    private ServiceBusProcessor? processor;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!transport.IsEnabled || environment.IsEnvironment("Testing"))
        {
            logger.LogInformation(
                "Azure Service Bus listener for {DtoType} on {QueueName} is disabled.",
                typeof(TDto).FullName,
                queueName);
            return;
        }

        this.processor = transport.CreateProcessor(queueName);
        this.processor.ProcessMessageAsync += ProcessMessageAsync;
        this.processor.ProcessErrorAsync += ProcessErrorAsync;

        await this.processor.StartProcessingAsync(cancellationToken);

        logger.LogInformation(
            "Started Azure Service Bus listener for {DtoType} on {QueueName}.",
            typeof(TDto).FullName,
            queueName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.processor is null)
        {
            return;
        }

        await this.processor.StopProcessingAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.processor is not null)
        {
            await this.processor.DisposeAsync();
        }
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var retryCount = ReadRetryCount(args.Message);
        var envelope = new TransportEnvelope(
            args.Message.Body,
            new MessageMetadata(
                args.Message.MessageId,
                args.Message.CorrelationId,
                args.Message.ReplyTo,
                queueName,
                retryCount,
                CloneApplicationProperties(args.Message)),
            "azure_service_bus",
            typeof(TDto),
            args.Message.DeliveryCount);
        var lifecycle = new AzureServiceBusMessageLifecycle(
            queueName,
            args.Message,
            args,
            transport,
            retryCount);

        await session.ProcessAsync(envelope, lifecycle, args.CancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Azure Service Bus listener for {DtoType} on {QueueName} failed during {ErrorSource}.",
            typeof(TDto).FullName,
            queueName,
            args.ErrorSource);
        return Task.CompletedTask;
    }

    private static int ReadRetryCount(ServiceBusReceivedMessage message)
    {
        if (!message.ApplicationProperties.TryGetValue("soundtrail-retry-count", out var value) || value is null)
        {
            return 0;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
            _ => 0
        };
    }

    private static IReadOnlyDictionary<string, object?> CloneApplicationProperties(ServiceBusReceivedMessage message)
    {
        return message.ApplicationProperties.ToDictionary(
            pair => pair.Key,
            pair => (object?)pair.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}
