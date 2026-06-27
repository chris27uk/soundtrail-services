using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Api.Features.SearchCatalog.Adapters;
using Soundtrail.Services.Api.Infrastructure.CompositionRoot;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Public.Projector.Infrastructure.CompositionRoot;
using Soundtrail.Services.Public.Projector.Infrastructure.Messaging;
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
    public async Task Given_orchestrator_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.Orchestrator",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseOrchestratorServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddOrchestratorAppServices(builder.Configuration);
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
    public async Task Given_public_projector_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Public.Projector",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UsePublicProjectorServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddPublicProjectorAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: true);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Given_scheduler_host_composition_when_starting_the_real_app_then_alive_endpoint_is_available()
    {
        await using var host = await DeployableStartupSmokeTestHost.StartAsync(
            "src/Soundtrail.Services.Enrichment.Scheduler",
            builder =>
            {
                builder.AddServiceDefaults();
                builder.Host.UseWolverine(opts =>
                {
                    opts.UseSchedulerServiceBusMessaging(builder.Configuration);
                    if (builder.Environment.IsDevelopment())
                    {
                        opts.StubAllExternalTransports();
                    }
                });
                builder.Services.AddSchedulerAppServices(builder.Configuration);
            },
            app => app.MapDefaultEndpoints(),
            useEmbeddedRaven: false);

        using var response = await host.Client.GetAsync("/alive");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
