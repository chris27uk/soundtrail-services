using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ApiServiceBusOptions = Soundtrail.Services.Api.Infrastructure.Messaging.ServiceBusOptions;
using OrchestratorServiceBusOptions = Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging.ServiceBusOptions;
using SchedulerServiceBusOptions = Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging.ServiceBusOptions;
using PublicProjectorServiceBusOptions = Soundtrail.Services.Public.Projector.Infrastructure.Messaging.ServiceBusOptions;
using WorkerServiceBusOptions = Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging.ServiceBusOptions;

namespace Soundtrail.Services.Tests.Integration.Startup;

public sealed class DeployableServiceBusConfigurationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    public void Given_api_appsettings_when_binding_service_bus_options_then_lookup_music_requests_queue_name_is_present()
    {
        var options = BindOptions<ApiServiceBusOptions>("src/Soundtrail.Services.Api/appsettings.json");

        options.CatalogSearchAttemptsQueueName.Should().Be("lookup-music-requests");
    }

    [Fact]
    public void Given_orchestrator_appsettings_when_binding_service_bus_options_then_all_required_queue_names_are_present()
    {
        var options = BindOptions<OrchestratorServiceBusOptions>("src/Soundtrail.Services.Enrichment.Orchestrator/appsettings.json");

        options.CatalogSearchAttemptsQueueName.Should().Be("lookup-music-requests");
        options.DiscoveryBacklogSchedulingQueueName.Should().Be("discovery-backlog-scheduling");
        options.MusicBrainzLookupQueueName.Should().Be("lookup-musicbrainz");
        options.PlaybackReferencesLookupQueueName.Should().Be("lookup-playback-references");
        options.EnrichmentResponsesQueueName.Should().Be("enrichment-responses");
        options.MusicTrackEventsQueueName.Should().Be("music-track-events");
    }

    [Fact]
    public void Given_scheduler_appsettings_when_binding_service_bus_options_then_backlog_scheduling_queue_name_is_present()
    {
        var options = BindOptions<SchedulerServiceBusOptions>("src/Soundtrail.Services.Enrichment.Scheduler/appsettings.json");

        options.DiscoveryBacklogSchedulingQueueName.Should().Be("discovery-backlog-scheduling");
    }

    [Fact]
    public void Given_worker_appsettings_when_binding_service_bus_options_then_all_required_queue_names_are_present()
    {
        var options = BindOptions<WorkerServiceBusOptions>("src/Soundtrail.Services.Enrichment.Worker/appsettings.json");

        options.MusicBrainzLookupQueueName.Should().Be("lookup-musicbrainz");
        options.PlaybackReferencesLookupQueueName.Should().Be("lookup-playback-references");
        options.EnrichmentResponsesQueueName.Should().Be("enrichment-responses");
    }

    [Fact]
    public void Given_public_projector_appsettings_when_binding_service_bus_options_then_music_track_events_queue_name_is_present()
    {
        var options = BindOptions<PublicProjectorServiceBusOptions>("src/Soundtrail.Services.Public.Projector/appsettings.json");

        options.MusicTrackEventsQueueName.Should().Be("music-track-events");
    }

    private static TOptions BindOptions<TOptions>(string relativePath) where TOptions : new()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(SolutionRoot, relativePath), optional: false)
            .Build();

        return configuration.GetSection("ServiceBus").Get<TOptions>() ?? new TOptions();
    }
}
