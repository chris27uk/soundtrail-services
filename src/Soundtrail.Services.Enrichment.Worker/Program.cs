using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.AppleLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.Execution.YouTubeMusicLookupExecution;
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

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.AppleLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.YouTubeMusicLookupQueueName)
        .ProcessInline();

    opts.PublishMessage<EnrichmentResponseDto>()
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
