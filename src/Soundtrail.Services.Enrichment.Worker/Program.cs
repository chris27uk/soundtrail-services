using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
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
    opts.Discovery.IncludeType<MusicBrainzLookupExecutionListener>();
    opts.Discovery.IncludeType<PlaybackReferencesLookupExecutionListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName)
        .ProcessInline();

    opts.PublishMessage<EnrichmentResponseDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName);
});

builder.Services.AddWorkerAppServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
