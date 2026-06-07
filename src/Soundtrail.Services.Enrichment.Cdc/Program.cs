using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Raven;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = Host.CreateApplicationBuilder(args);

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

builder.UseWolverine(opts =>
{
    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.PublishMessage<ResolveApplePlaybackReferenceCommand>()
        .ToAzureServiceBusQueue(serviceBusOptions.AppleLookupQueueName);

    opts.PublishMessage<ResolveYouTubeMusicPlaybackReferenceCommand>()
        .ToAzureServiceBusQueue(serviceBusOptions.YouTubeMusicLookupQueueName);
});

builder.Services.AddCdcRavenDocumentStore(builder.Configuration);
builder.Services.AddScoped<MusicTrackEventCommandHandler>();
builder.Services.AddHostedService<MusicTrackEventSubscriptionHostedService>();

var host = builder.Build();

await host.RunAsync();
