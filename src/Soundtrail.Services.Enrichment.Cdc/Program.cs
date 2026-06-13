using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Cdc;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
if (string.IsNullOrWhiteSpace(serviceBusOptions.MusicTrackEventsQueueName))
{
    throw new InvalidOperationException("ServiceBus:MusicTrackEventsQueueName is required.");
}
var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

if (useDevelopmentEmulator)
{
    builder.Services.UseWolverineSoloMode();
}

builder.Host.UseWolverine(opts =>
{
    opts.UseRuntimeCompilation();
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
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
});

builder.Services.AddCdcAppServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
