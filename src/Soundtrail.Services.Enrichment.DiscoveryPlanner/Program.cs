using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.Worker;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Scheduling;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search.Resolution;
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
    opts.Discovery.IncludeType<LookupMusicRequestListener>();
    opts.Discovery.IncludeType<DiscoveryBacklogSchedulingListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LookupMusicRequestsQueueName)
        .ProcessInline();

    opts.PublishMessage<ResolveCanonicalMetadataCommandDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);
});

builder.Services.AddSchedulerRavenDocumentStore(builder.Configuration);
builder.Services.AddSchedulerServiceBus(builder.Configuration);
builder.Services.Configure<LookupPlanningOptions>(builder.Configuration.GetSection(LookupPlanningOptions.SectionName));
builder.Services.AddSingleton<DiscoveryPriorityPolicy>();
builder.Services.AddSingleton<MusicCatalogResolutionPolicy>();
builder.Services.AddScoped<LookupMusicRequestHandler>();
builder.Services.AddScoped<DiscoveryBacklogScheduler>();
builder.Services.AddScoped<LookupMusicRequestListener>();
builder.Services.AddScoped<DiscoveryBacklogSchedulingListener>();
builder.Services.AddHostedService<LookupPlanningSweepHostedService>();

var host = builder.Build();

await host.RunAsync();
