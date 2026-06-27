using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddScoped<ICommandBus, WolverineCommandBus>();
        return services;
    }

    public static WolverineOptions UseWorkerServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
        opts.Discovery.DisableConventionalDiscovery();
        opts.Discovery.IncludeType<LookupTrackMetadataListener>();
        opts.Discovery.IncludeType<LookupStreamingLocationsListener>();
        opts.Policies.AutoApplyTransactions();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

        if (!useDevelopmentEmulator)
        {
            opts.UseRavenDbPersistence();
        }

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

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName)
            .ProcessInline();

        opts.PublishMessage<MusicCatalogLookupAttemptedDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName);

        return opts;
    }
}
