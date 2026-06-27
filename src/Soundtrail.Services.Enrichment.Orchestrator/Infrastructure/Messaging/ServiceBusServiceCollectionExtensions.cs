using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownAlbumRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownArtistRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownTrackRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnStreamingLocationsRequired.Adapters;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddOrchestratorServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddScoped<ICommandBus, WolverineCommandBus>();
        return services;
    }

    public static WolverineOptions UseOrchestratorServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
        opts.Discovery.DisableConventionalDiscovery();
        opts.Discovery.IncludeType<SearchCatalogRequestedListener>();
        opts.Discovery.IncludeType<KnownArtistRequestedListener>();
        opts.Discovery.IncludeType<KnownAlbumRequestedListener>();
        opts.Discovery.IncludeType<KnownTrackRequestedListener>();
        opts.Discovery.IncludeType<NextMusicTracksRequestedForLookupListener>();
        opts.Discovery.IncludeType<AssessMusicTrackListener>();
        opts.Discovery.IncludeType<MusicCatalogLookupAttemptedListener>();
        opts.Discovery.IncludeType<StreamingLocationsRequiredListener>();
        opts.Policies.AutoApplyTransactions();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

        if (!useDevelopmentEmulator)
        {
            opts.UseRavenDbPersistence();
        }

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

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.CatalogSearchAttemptsQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.DiscoveryBacklogSchedulingQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.AssessMusicTrackQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName)
            .ProcessInline();

        opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicTrackEventsQueueName)
            .ProcessInline();

        opts.PublishMessage<LookupTrackMetadataCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        opts.PublishMessage<LookupStreamingLocationsCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);

        return opts;
    }
}
