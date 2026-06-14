using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Cdc;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Wolverine;

namespace Soundtrail.Services.Tests.Integration.Startup;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class DeployableStartupSmokeTests
{
    [Fact]
    public async Task Given_api_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Api",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseApiServiceBusMessaging(builder.Configuration, builder.Environment);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddApiAppServices(builder.Configuration, builder.Environment);
            },
            app =>
            {
                app.MapDefaultEndpoints();
                app.MapSearchCatalogEndpoints();
            },
            useEmbeddedRaven: true);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_discovery_planner_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.DiscoveryPlanner",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseDiscoveryPlannerServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddDiscoveryPlannerAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: true);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_worker_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.Worker",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseWorkerServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddWorkerAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: true);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_cdc_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.Cdc",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseCdcServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddCdcAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: true);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_music_track_lookup_coordinator_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.MusicTrackLookupCoordinator",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseMusicTrackLookupCoordinatorServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddMusicTrackLookupCoordinatorAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: false);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
