using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Responder.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Responder.Infrastructure.Raven;
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
    opts.Discovery.IncludeType<EnrichmentResponseListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName)
        .ProcessInline();
});

builder.Services.AddResponderRavenDocumentStore(builder.Configuration);
builder.Services.AddScoped<ApplyEnrichmentResponseHandler>();
builder.Services.AddScoped<EnrichmentResponseListener>();

var host = builder.Build();

await host.RunAsync();
