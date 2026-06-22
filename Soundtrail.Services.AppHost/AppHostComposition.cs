using Aspire.Hosting;
using Microsoft.Extensions.Configuration;

namespace Soundtrail.Services.AppHost;

public static class AppHostComposition
{
    public static void Configure(IDistributedApplicationBuilder builder, string? contentRootPath = null)
    {
        var resolvedContentRootPath = contentRootPath ?? builder.Environment.ContentRootPath;
        AppHostStartupValidator.Validate(builder.Configuration, resolvedContentRootPath);

        var serviceBus = builder.AddConnectionString("servicebus");
        var useProviderStubs = builder.Configuration.GetValue("LocalDevelopment:UseProviderStubs", false);
        var useServiceBusEmulator = builder.Configuration.GetValue("LocalDevelopment:UseServiceBusEmulator", false);

        var ravenDb = builder.AddContainer("ravendb", "ravendb/ravendb", "7.1-ubuntu-latest")
            .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
            .WithEnvironment("RAVEN_Setup_Mode", "None")
            .WithEnvironment("RAVEN_Security_UnsecuredAccessAllowed", "PublicNetwork")
            .WithEnvironment("RAVEN_License_Eula_Accepted", "true");

        var serviceBusSqlPassword = builder.Configuration["ServiceBusEmulator:SqlPassword"];
        var serviceBusEmulatorSql = useServiceBusEmulator
            ? builder.AddContainer("mssql", "mcr.microsoft.com/mssql/server", "2022-latest")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_PID", "Developer")
                .WithEnvironment("MSSQL_SA_PASSWORD", serviceBusSqlPassword!)
                .WithEndpoint(port: 1433, targetPort: 1433, name: "sql")
            : null;

        var serviceBusEmulator = useServiceBusEmulator
            ? builder.AddContainer("servicebus-emulator", "mcr.microsoft.com/azure-messaging/servicebus-emulator", "latest")
                .WithBindMount(
                    Path.Combine(resolvedContentRootPath, "servicebus-emulator", "Config.json"),
                    "/ServiceBus_Emulator/ConfigFiles/Config.json",
                    isReadOnly: true)
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("SQL_SERVER", "mssql")
                .WithEnvironment("MSSQL_SA_PASSWORD", serviceBusSqlPassword!)
                .WithEnvironment("EMULATOR_HTTP_PORT", "5300")
                .WithEnvironment("SQL_WAIT_INTERVAL", "15")
                .WithEndpoint(port: 5672, targetPort: 5672, name: "amqp")
                .WithHttpEndpoint(port: 5300, targetPort: 5300, name: "http")
                .WithHttpHealthCheck("/health")
                .WaitFor(serviceBusEmulatorSql!)
            : null;

        var providerStubs = useProviderStubs
            ? builder.AddContainer("provider-stubs", "wiremock/wiremock", "3.9.1")
                .WithHttpEndpoint(port: 9090, targetPort: 8080, name: "http")
                .WithBindMount(
                    Path.Combine(resolvedContentRootPath, "wiremock"),
                    "/home/wiremock",
                    isReadOnly: true)
            : null;

        var api = builder.AddProject<Projects.Soundtrail_Services_Api>("soundtrail-services-api")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("ServiceBus__CatalogSearchAttemptsQueueName", "lookup-music-requests")
            .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (useProviderStubs)
        {
            api = api.WithEnvironment("LocalDevelopment__SeedAsyncLookupTrack", "true");
        }

        if (serviceBusEmulator is not null)
        {
            api = api.WaitFor(serviceBusEmulator);
        }

        var publicProjector = builder.AddProject<Projects.Soundtrail_Services_Public_Projector>("soundtrail-services-public-projector")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("ServiceBus__MusicTrackEventsQueueName", "music-track-events")
            .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (serviceBusEmulator is not null)
        {
            publicProjector = publicProjector.WaitFor(serviceBusEmulator);
        }

        var internalProjector = builder.AddProject<Projects.Soundtrail_Services_Internal_Projector>("soundtrail-services-internal-projector")
            .WithHttpEndpoint(name: "http")
            .WaitFor(ravenDb)
            .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
            .WithEnvironment("RavenDb__Database", "soundtrail");

        var orchestrator = builder.AddProject<Projects.Soundtrail_Services_Enrichment_Orchestrator>("soundtrail-services-enrichment-orchestrator")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("ServiceBus__CatalogSearchAttemptsQueueName", "lookup-music-requests")
            .WithEnvironment("ServiceBus__MusicBrainzLookupQueueName", "lookup-musicbrainz")
            .WithEnvironment("ServiceBus__PlaybackReferencesLookupQueueName", "lookup-playback-references")
            .WithEnvironment("ServiceBus__EnrichmentResponsesQueueName", "enrichment-responses")
            .WithEnvironment("ServiceBus__MusicTrackEventsQueueName", "music-track-events")
            .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (serviceBusEmulator is not null)
        {
            orchestrator = orchestrator.WaitFor(serviceBusEmulator);
        }

        var worker = builder.AddProject<Projects.Soundtrail_Services_Enrichment_Worker>("soundtrail-services-enrichment-worker")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("ServiceBus__MusicBrainzLookupQueueName", "lookup-musicbrainz")
            .WithEnvironment("ServiceBus__PlaybackReferencesLookupQueueName", "lookup-playback-references")
            .WithEnvironment("ServiceBus__EnrichmentResponsesQueueName", "enrichment-responses")
            .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (serviceBusEmulator is not null)
        {
            worker = worker.WaitFor(serviceBusEmulator);
        }

        if (providerStubs is not null)
        {
            worker = worker
                .WithEnvironment("MusicBrainz__BaseUrl", providerStubs.GetEndpoint("http"))
                .WithEnvironment("Odesli__BaseUrl", providerStubs.GetEndpoint("http"));
        }
    }
}
