using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Composition;

[Autodiscover]
public sealed class OnLookupWorkReadyFeature : IOrchestratorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWolverineCommandBus();
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.TryAddScoped<IHandler<DispatchLookupWork>, LookupWorkReadyHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
            return;
        }

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (!serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString())
        {
            transport.AutoProvision();
        }

        options.ListenToAzureServiceBusQueue(serviceBusOptions.DispatchLookupWorkQueueName)
            .ProcessInline();

        options.PublishMessage<LookupMusicbrainzSearchResultsCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        options.PublishMessage<LookupMusicbrainzArtistAlbumsCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        options.PublishMessage<LookupMusicbrainzArtistTracksCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        options.PublishMessage<LookupMusicbrainzAlbumTracksCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName);

        options.PublishMessage<LookupStreamingLocationByIsrcCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);

        options.PublishMessage<LookupStreamingLocationByTrackMetadataCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName);

        options.PublishMessage<LookupPlaylistTracksByProviderCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.MusicPlaylistLookupQueueName);
    }
}
