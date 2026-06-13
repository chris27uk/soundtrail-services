using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.DiscoveryPlanner;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

if (useDevelopmentEmulator)
{
    builder.Services.UseWolverineSoloMode();
}

builder.Host.UseWolverine(opts =>
{
    opts.UseRuntimeCompilation();
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType<LookupMusicRequestListener>();
    opts.Discovery.IncludeType<DiscoveryBacklogSchedulingListener>();
    opts.Discovery.IncludeType<EnrichmentResponseListener>();
    if (!useDevelopmentEmulator)
    {
        opts.UseRavenDbPersistence();
    }
    opts.Policies.AutoApplyTransactions();

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

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
