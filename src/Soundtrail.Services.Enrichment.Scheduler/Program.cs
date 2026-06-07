using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Enrichment.Features.BacklogScheduling;
using Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;
using Soundtrail.Services.Enrichment.Features.JustInTimeScheduling;
using Soundtrail.Services.Enrichment.Shared.Orchestration;
using Soundtrail.Services.Enrichment.Shared.Prioritisation;
using Soundtrail.Services.Enrichment.Shared.Search.Resolution;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;
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
    opts.Discovery.IncludeType<EnrichmentResponseListener>();
    opts.Discovery.IncludeType<MusicTrackBusinessIntentListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.LookupMusicRequestsQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName)
        .ProcessInline();

    opts.PublishMessage<HighPriorityResolveCanonicalMetadataCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.HighPriorityMusicBrainzLookupQueueName);

    opts.PublishMessage<LowPriorityResolveCanonicalMetadataCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.LowPriorityMusicBrainzLookupQueueName);

    opts.PublishMessage<HighPriorityResolveApplePlaybackReferenceCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.HighPriorityAppleLookupQueueName);

    opts.PublishMessage<LowPriorityResolveApplePlaybackReferenceCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.LowPriorityAppleLookupQueueName);

    opts.PublishMessage<HighPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.HighPriorityYouTubeMusicLookupQueueName);

    opts.PublishMessage<LowPriorityResolveYouTubeMusicPlaybackReferenceCommandMessage>()
        .ToAzureServiceBusQueue(serviceBusOptions.LowPriorityYouTubeMusicLookupQueueName);
});

builder.Services.AddSchedulerRavenDocumentStore(builder.Configuration);
builder.Services.AddSchedulerServiceBus(builder.Configuration);
builder.Services.Configure<LookupPlanningOptions>(builder.Configuration.GetSection(LookupPlanningOptions.SectionName));
builder.Services.AddSingleton<DiscoveryPriorityPolicy>();
builder.Services.AddSingleton<MusicCatalogResolutionPolicy>();
builder.Services.AddScoped<ApplyEnrichmentResponseHandler>();
builder.Services.AddScoped<LookupMusicRequestHandler>();
builder.Services.AddScoped<DiscoveryBacklogScheduler>();
builder.Services.AddScoped<LookupMusicRequestListener>();
builder.Services.AddScoped<EnrichmentResponseListener>();
builder.Services.AddScoped<DiscoveryBacklogSchedulingListener>();
builder.Services.AddHostedService<LookupPlanningSweepHostedService>();

var host = builder.Build();

await host.RunAsync();
