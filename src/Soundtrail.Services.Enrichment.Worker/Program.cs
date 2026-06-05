using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.Execution;
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
    opts.Discovery.IncludeType<LookupExecutionListener>();
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
});

builder.Services.AddWorkerRavenDocumentStore(builder.Configuration);
builder.Services.AddScoped<LookupExecutionHandler>();
builder.Services.AddScoped<LookupExecutionListener>();

var host = builder.Build();

await host.RunAsync();
