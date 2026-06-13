using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
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
    opts.Discovery.IncludeType<MusicBrainzLookupExecutionListener>();
    opts.Discovery.IncludeType<PlaybackReferencesLookupExecutionListener>();
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

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName)
        .ProcessInline();

    opts.PublishMessage<EnrichmentResponseDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName);
});

builder.Services.AddWorkerAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
