using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;

namespace Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddCdcServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        return services;
    }

    public static WolverineOptions UseCdcServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        if (string.IsNullOrWhiteSpace(serviceBusOptions.MusicTrackEventsQueueName))
        {
            throw new InvalidOperationException("ServiceBus:MusicTrackEventsQueueName is required.");
        }

        var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

        var transport = opts.UseAzureServiceBus(serviceBusOptions.ConnectionString);
        if (useDevelopmentEmulator)
        {
            transport.SystemQueuesAreEnabled(false);
        }
        else
        {
            transport.AutoProvision()
                .EnableWolverineControlQueues();
        }

        opts.PublishMessage<PlaybackReferencesResolutionRequiredMessageDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicTrackEventsQueueName);

        return opts;
    }
}
