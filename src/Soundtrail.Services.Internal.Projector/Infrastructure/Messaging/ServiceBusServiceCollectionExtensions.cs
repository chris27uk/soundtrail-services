using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddInternalProjectorServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddScoped<ICommandBus, WolverineCommandBus>();
        return services;
    }

    public static WolverineOptions UseInternalProjectorServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
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

        opts.PublishMessage<AssessMusicTrackCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.AssessMusicTrackQueueName);

        opts.PublishMessage<CatalogSearchAttemptDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.CatalogSearchAttemptsQueueName);

        opts.PublishMessage<LookupTrackMetadataCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        opts.PublishMessage<LookupStreamingLocationsCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);

        return opts;
    }
}
