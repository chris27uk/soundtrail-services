using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Soundtrail.Adapters.Messaging;

public static class AzureServiceBusMessageProcessingServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusMessageProcessing(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddAzureServiceBusCommandBus();
        services.TryAddSingleton<IMessageBodyDeserializer, SystemTextJsonMessageBodyDeserializer>();
        services.TryAddSingleton<ExponentialRetryPolicy>();
        services.TryAddTransient(typeof(IncomingMessageSession<,>));

        return services;
    }

    public static IServiceCollection AddAzureServiceBusListener<TDto, TDomain>(
        this IServiceCollection services,
        string queueName)
        where TDto : class
        where TDomain : class
    {
        services.AddSingleton<IHostedService>(
            sp => new AzureServiceBusMessageListenerHostedService<TDto, TDomain>(
                queueName,
                sp.GetRequiredService<AzureServiceBusMessageTransport>(),
                sp.GetRequiredService<IncomingMessageSession<TDto, TDomain>>(),
                sp.GetRequiredService<IHostEnvironment>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AzureServiceBusMessageListenerHostedService<TDto, TDomain>>>()));

        return services;
    }
}
