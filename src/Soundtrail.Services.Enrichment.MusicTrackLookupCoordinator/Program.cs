using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.Worker;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Features.Orchestration;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
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
    opts.Discovery.IncludeType<MusicTrackEventListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicTrackEventsQueueName)
        .ProcessInline();

    opts.PublishMessage<ResolveApplePlaybackReferenceCommandDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.AppleLookupQueueName);

    opts.PublishMessage<ResolveYouTubeMusicPlaybackReferenceCommandDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.YouTubeMusicLookupQueueName);
});

builder.Services.AddScoped<MusicTrackEventCommandHandler>();
builder.Services.AddScoped<MusicTrackEventListener>();

var host = builder.Build();

await host.RunAsync();
