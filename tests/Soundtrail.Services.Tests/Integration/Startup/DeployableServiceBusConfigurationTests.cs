using FluentAssertions;
using Microsoft.Extensions.Configuration;
using ApiServiceBusOptions = Soundtrail.Services.Api.Infrastructure.Messaging.ServiceBusOptions;
using CdcServiceBusOptions = Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging.ServiceBusOptions;
using CoordinatorServiceBusOptions = Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging.ServiceBusOptions;
using DiscoveryPlannerServiceBusOptions = Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging.ServiceBusOptions;
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
    public void Given_discovery_planner_appsettings_when_binding_service_bus_options_then_all_required_queue_names_are_present()
    {
        var options = BindOptions<DiscoveryPlannerServiceBusOptions>("src/Soundtrail.Services.Enrichment.DiscoveryPlanner/appsettings.json");

        options.CatalogSearchAttemptsQueueName.Should().Be("lookup-music-requests");
        options.MusicBrainzLookupQueueName.Should().Be("lookup-musicbrainz");
        options.PlaybackReferencesLookupQueueName.Should().Be("lookup-playback-references");
        options.EnrichmentResponsesQueueName.Should().Be("enrichment-responses");
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
    public void Given_cdc_appsettings_when_binding_service_bus_options_then_music_track_events_queue_name_is_present()
    {
        var options = BindOptions<CdcServiceBusOptions>("src/Soundtrail.Services.Enrichment.Cdc/appsettings.json");

        options.MusicTrackEventsQueueName.Should().Be("music-track-events");
    }

    [Fact]
    public void Given_music_track_lookup_coordinator_appsettings_when_binding_service_bus_options_then_required_queue_names_are_present()
    {
        var options = BindOptions<CoordinatorServiceBusOptions>("src/Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator/appsettings.json");

        options.MusicTrackEventsQueueName.Should().Be("music-track-events");
        options.PlaybackReferencesLookupQueueName.Should().Be("lookup-playback-references");
    }

    private static TOptions BindOptions<TOptions>(string relativePath) where TOptions : new()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(SolutionRoot, relativePath), optional: false)
            .Build();

        return configuration.GetSection("ServiceBus").Get<TOptions>() ?? new TOptions();
    }
}
