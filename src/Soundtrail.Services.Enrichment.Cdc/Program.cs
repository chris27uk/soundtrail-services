using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Events;
using Soundtrail.Services.Enrichment.Cdc;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

builder.UseWolverine(opts =>
{
    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.PublishMessage<PlaybackReferencesResolutionRequiredMessageDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicTrackEventsQueueName);
});

builder.Services.AddCdcAppServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
