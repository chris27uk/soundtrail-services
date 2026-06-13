using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

if (useDevelopmentEmulator)
{
    builder.Services.UseWolverineSoloMode();
}

builder.Host.UseWolverine(opts =>
{
    opts.UseRuntimeCompilation();
    opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType<MusicTrackEventListener>();
    if (!useDevelopmentEmulator)
    {
        opts.UseRavenDbPersistence();
    }
    opts.Policies.AutoApplyTransactions();

    var transport = opts.UseAzureServiceBus(serviceBusOptions.ConnectionString);
    if (useDevelopmentEmulator)
    {
        transport.SystemQueuesAreEnabled(false);
    }
    else
    {
        transport.AutoProvision()
            .EnableWolverineControlQueues();
    }

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicTrackEventsQueueName)
        .ProcessInline();

    opts.PublishMessage<ResolvePlaybackReferencesCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);
});

builder.Services.AddMusicTrackLookupCoordinatorAppServices();

var app = builder.Build();
app.MapDefaultEndpoints();

await app.RunAsync();
