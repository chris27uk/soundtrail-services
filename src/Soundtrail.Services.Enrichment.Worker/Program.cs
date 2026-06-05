using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.Execution.AppleLookupExecution;
using Soundtrail.Services.Enrichment.Features.Execution.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Features.Execution.YouTubeMusicLookupExecution;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = Host.CreateApplicationBuilder(args);

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

builder.UseWolverine(opts =>
{
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType<MusicBrainzLookupExecutionListener>();
    opts.Discovery.IncludeType<AppleLookupExecutionListener>();
    opts.Discovery.IncludeType<YouTubeMusicLookupExecutionListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.HighPriorityMusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LowPriorityMusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.HighPriorityAppleLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LowPriorityAppleLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.HighPriorityYouTubeMusicLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LowPriorityYouTubeMusicLookupQueueName)
        .ProcessInline();

    opts.PublishMessage<EnrichmentResponse>()
        .ToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName);
});

builder.Services.AddWorkerRavenDocumentStore(builder.Configuration);
builder.Services.AddScoped<ExecuteMusicBrainzLookupHandler>();
builder.Services.AddScoped<ExecuteAppleLookupHandler>();
builder.Services.AddScoped<ExecuteYouTubeMusicLookupHandler>();
builder.Services.AddScoped<MusicBrainzLookupExecutionListener>();
builder.Services.AddScoped<AppleLookupExecutionListener>();
builder.Services.AddScoped<YouTubeMusicLookupExecutionListener>();

var host = builder.Build();

await host.RunAsync();
