using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ApplyLookupExecutionReport.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        return services;
    }

    public static WolverineOptions UseDiscoveryPlannerServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
        opts.Discovery.DisableConventionalDiscovery();
        opts.Discovery.IncludeType<CatalogSearchAttemptListener>();
        opts.Discovery.IncludeType<DiscoveryBacklogSchedulingListener>();
        opts.Discovery.IncludeType<EnrichmentResponseListener>();
        opts.Discovery.IncludeType<LookupExecutionReportListener>();
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

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.CatalogSearchAttemptsQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName)
            .ProcessInline();

        opts.PublishMessage<LookupCanonicalMusicMetadataCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        opts.PublishMessage<ResolvePlaybackReferencesCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);

        return opts;
    }
}
