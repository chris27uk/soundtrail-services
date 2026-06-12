using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

builder.UseWolverine(opts =>
{
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType<LookupMusicRequestListener>();
    opts.Discovery.IncludeType<DiscoveryBacklogSchedulingListener>();
    opts.Discovery.IncludeType<EnrichmentResponseListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LookupMusicRequestsQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName)
        .ProcessInline();

    opts.PublishMessage<LookupCanonicalMusicMetadataCommandDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

    opts.PublishMessage<ResolvePlaybackReferencesCommandDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);
});

builder.Services.AddDiscoveryPlannerAppServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
