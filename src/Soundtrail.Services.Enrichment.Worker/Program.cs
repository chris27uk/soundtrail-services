using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Planning;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
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
        .SystemQueuesAreEnabled(false);

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LookupMusicRequestsQueueName)
        .ProcessInline();

    opts.PublishMessage<HighPriorityLookupMusicCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.LookupMusicHighQueueName);

    opts.PublishMessage<LowPriorityLookupMusicCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.LookupMusicLowQueueName);
});

builder.Services.AddWorkerRavenDocumentStore(builder.Configuration);
builder.Services.AddWorkerServiceBus(builder.Configuration);
builder.Services.Configure<LookupPlanningOptions>(builder.Configuration.GetSection(LookupPlanningOptions.SectionName));
builder.Services.AddSingleton<LookupPlanner>();
builder.Services.AddSingleton<MusicCatalogResolutionPolicy>();
builder.Services.AddSingleton<LookupSchedulerHandler>();
builder.Services.AddSingleton<LookupSchedulerOrchestrator>();
builder.Services.AddSingleton<LookupPlanningSweep>();
builder.Services.AddHostedService<LookupPlanningSweepHostedService>();

var host = builder.Build();

await host.RunAsync();
