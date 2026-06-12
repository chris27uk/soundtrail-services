using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

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

    opts.PublishMessage<ResolvePlaybackReferencesCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);
});

builder.Services.AddMusicTrackLookupCoordinatorAppServices();

var host = builder.Build();

await host.RunAsync();
