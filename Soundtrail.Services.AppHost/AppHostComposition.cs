using Aspire.AsbEmulatorUi.Integration;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Microsoft.Extensions.Configuration;
using Projects;

namespace Soundtrail.Services.AppHost;

public static class AppHostComposition
{
    public static void Configure(IDistributedApplicationBuilder builder, string? contentRootPath = null)
    {
        const string ravenDbDashboardUrl = "http://ravendb.localhost/studio/index.html";
        const string ravenDbInternalUrl = "http://localhost:8080";
        const string ravenDbListenUrl = "http://0.0.0.0:8080";
        const string ravenDbListenTcpUrl = "tcp://0.0.0.0:38888";
        const string ravenDbPublicTcpUrl = "tcp://localhost:38888";
        const string apiPublicUrl = "http://api.localhost";
        const string streamBrowserPublicUrl = "http://streams.localhost";

        var resolvedContentRootPath = contentRootPath ?? builder.Environment.ContentRootPath;
        AppHostStartupValidator.Validate(builder.Configuration, resolvedContentRootPath);

        var useProviderStubs = builder.Configuration.GetValue("LocalDevelopment:UseProviderStubs", false);
        var useServiceBusEmulator = builder.Configuration.GetValue("LocalDevelopment:UseServiceBusEmulator", false);
        var useBlobStorageEmulator = builder.Configuration.GetValue("LocalDevelopment:UseBlobStorageEmulator", false);
        var ravenDbLicensePath = builder.Configuration["RavenDb:LicensePath"];
        var otelServiceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION");
        if (string.IsNullOrWhiteSpace(otelServiceVersion))
        {
            otelServiceVersion = "0.0.0-local";
        }

        var serviceBusEmulatorConfigPath = Path.Combine(
            resolvedContentRootPath,
            "servicebus-emulator",
            "Config.json");

        var redis = builder.AddContainer("redis", "redis", "7-alpine")
            .WithEndpoint(port: 6379, targetPort: 6379, name: "tcp");

        var ravenDb = builder.AddContainer("ravendb", "ravendb/ravendb", "7.1-ubuntu-latest")
            .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
            .WithUrlForEndpoint("http", url => url.Url = ravenDbDashboardUrl)
            .WithEndpoint(port: 38888, targetPort: 38888, name: "tcp")
            .WithEnvironment("RAVEN_Setup_Mode", "None")
            .WithEnvironment("RAVEN_ServerUrl", ravenDbListenUrl)
            .WithEnvironment("RAVEN_ServerUrl_Tcp", ravenDbListenTcpUrl)
            .WithEnvironment("RAVEN_PublicServerUrl", ravenDbInternalUrl)
            .WithEnvironment("RAVEN_PublicServerUrl_Tcp", ravenDbPublicTcpUrl)
            .WithEnvironment("RAVEN_Security_UnsecuredAccessAllowed", "PublicNetwork")
            .WithEnvironment("RAVEN_License_Eula_Accepted", "true");

        if (!string.IsNullOrWhiteSpace(ravenDbLicensePath))
        {
            ravenDb = ravenDb
                .WithBindMount(
                    ravenDbLicensePath,
                    "/run/secrets/ravendb-license.json",
                    isReadOnly: true)
                .WithEnvironment("RAVEN_License_Path", "/run/secrets/ravendb-license.json");
        }
        
        IResourceBuilder<IResourceWithConnectionString> serviceBus;
        if (useServiceBusEmulator)
        {
            var serviceBusResource = builder.AddAzureServiceBus("servicebus");
            serviceBusResource.RunAsEmulator(c => c
                .WithLifetime(ContainerLifetime.Persistent)
                .WithConfigurationFile(serviceBusEmulatorConfigPath)
                .WithHostPort(5672));

            builder.AddAsbEmulatorUi("servicebus-ui", serviceBusResource);

            serviceBus = serviceBusResource;
        }
        else
        {
            serviceBus = builder.AddConnectionString("servicebus");
        }

        IResourceBuilder<IResourceWithConnectionString>? musicBrainzDumpBlobs = null;
        if (useBlobStorageEmulator)
        {
            musicBrainzDumpBlobs = builder.AddAzureStorage("storage")
                .RunAsEmulator(azurite => azurite.WithLifetime(ContainerLifetime.Persistent))
                .AddBlobs("musicbrainz-dumps");
        }

        var providerStubs = useProviderStubs
            ? builder.AddContainer("provider-stubs", "wiremock/wiremock", "3.9.1")
                .WithHttpEndpoint(port: 9090, targetPort: 8080, name: "http")
                .WithBindMount(
                    Path.Combine(resolvedContentRootPath, "wiremock"),
                    "/home/wiremock",
                    isReadOnly: true)
            : null;

        var api = builder.AddProject<Soundtrail_Services_Api>("soundtrail-api")
            .WithHttpEndpoint(port: 8081, targetPort: 8081, name: "http", isProxied: false)
            .WithUrlForEndpoint("http", url => url.Url = apiPublicUrl)
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail");

        builder.AddProject<Soundtrail_Services_StreamBrowser>("soundtrail-stream-browser")
            .WithHttpEndpoint(port: 8787, targetPort: 8787, name: "http", isProxied: false)
            .WithUrlForEndpoint("http", url => url.Url = streamBrowserPublicUrl)
            .WaitFor(ravenDb)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (useProviderStubs)
        {
            api = api.WithEnvironment("LocalDevelopment__SeedAsyncLookupTrack", "true");
        }

        if (useServiceBusEmulator)
        {
            api = api.WaitFor(serviceBus);
        }

        var projector = builder.AddProject<Soundtrail_Services_Projector>("soundtrail-projector")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (useServiceBusEmulator)
        {
            projector = projector.WaitFor(serviceBus);
        }

        var musicBrainzDumpSourceDirectory = Path.Combine(
            resolvedContentRootPath,
            "testdata",
            "musicbrainz-dump-source");
        var musicBrainzDumpCacheDirectory = Path.Combine(
            resolvedContentRootPath,
            "testdata",
            "musicbrainz-dump-cache");
        Directory.CreateDirectory(musicBrainzDumpCacheDirectory);

        var musicBrainzDumpSource = builder.AddContainer("musicbrainz-dump-source", "caddy", "2.9-alpine")
            .WithHttpEndpoint(targetPort: 80, name: "http")
            .WithBindMount(
                musicBrainzDumpSourceDirectory,
                "/srv",
                isReadOnly: true)
            .WithBindMount(
                Path.Combine(resolvedContentRootPath, "caddy", "MusicBrainzDumpSource.Caddyfile"),
                "/etc/caddy/Caddyfile",
                isReadOnly: true);

        var scheduler = builder.AddProject<Soundtrail_Services_Enrichment_Scheduler>("soundtrail-scheduler")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WaitFor(musicBrainzDumpSource)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail")
            .WithEnvironment("MusicBrainzDump__BaseUrl", musicBrainzDumpSource.GetEndpoint("http"));

        if (useServiceBusEmulator)
        {
            scheduler = scheduler.WaitFor(serviceBus);
        }

        var catalogImport = builder.AddProject<Soundtrail_Services_Enrichment_CatalogImport>("soundtrail-catalog-import")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WaitFor(musicBrainzDumpSource)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail")
            .WithEnvironment("MusicBrainzDump__BaseUrl", musicBrainzDumpSource.GetEndpoint("http"))
            .WithEnvironment("MusicBrainzDump__ArchiveDirectory", musicBrainzDumpCacheDirectory);

        if (useBlobStorageEmulator && musicBrainzDumpBlobs is not null)
        {
            catalogImport = catalogImport
                .WithReference(musicBrainzDumpBlobs)
                .WaitFor(musicBrainzDumpBlobs)
                .WithEnvironment("MusicBrainzDump__Storage", "Blob");
        }
        else
        {
            catalogImport = catalogImport.WithEnvironment("MusicBrainzDump__Storage", "Local");
        }

        if (useServiceBusEmulator)
        {
            catalogImport = catalogImport.WaitFor(serviceBus);
        }

        var orchestrator = builder.AddProject<Soundtrail_Services_Enrichment_Orchestrator>("soundtrail-orchestrator")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (useServiceBusEmulator)
        {
            orchestrator = orchestrator.WaitFor(serviceBus);
        }

        var worker = builder.AddProject<Soundtrail_Services_Enrichment_Worker>("soundtrail-worker")
            .WithHttpEndpoint(name: "http")
            .WithReference(serviceBus)
            .WaitFor(ravenDb)
            .WaitFor(redis)
            .WithEnvironment("OTEL_SERVICE_VERSION", otelServiceVersion)
            .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
            .WithEnvironment("ConnectionStrings__Redis", $"{redis.GetEndpoint("tcp").Property(EndpointProperty.Host)}:{redis.GetEndpoint("tcp").Property(EndpointProperty.Port)},abortConnect=false")
            .WithEnvironment("RavenDb__Urls__0", ravenDbInternalUrl)
            .WithEnvironment("RavenDb__Database", "soundtrail");

        if (useServiceBusEmulator)
        {
            worker = worker.WaitFor(serviceBus);
        }

        if (providerStubs is not null)
        {
            worker = worker
                .WithEnvironment("Kworb__BaseUrl", providerStubs.GetEndpoint("http"))
                .WithEnvironment("MusicBrainz__BaseUrl", providerStubs.GetEndpoint("http"))
                .WithEnvironment("Odesli__BaseUrl", providerStubs.GetEndpoint("http"))
                .WithEnvironment("SourceBudgets__Kworb__MaxRequests", "100000")
                .WithEnvironment("SourceBudgets__Kworb__WindowSeconds", "60")
                .WithEnvironment("SourceBudgets__Kworb__SafetyMarginPercent", "0")
                .WithEnvironment("SourceBudgets__Kworb__MinimumSpacingSeconds", "0")
                .WithEnvironment("SourceBudgets__MusicBrainz__MaxRequests", "100000")
                .WithEnvironment("SourceBudgets__MusicBrainz__WindowSeconds", "60")
                .WithEnvironment("SourceBudgets__MusicBrainz__SafetyMarginPercent", "0")
                .WithEnvironment("SourceBudgets__MusicBrainz__MinimumSpacingSeconds", "0")
                .WithEnvironment("SourceBudgets__Odesli__MaxRequests", "100000")
                .WithEnvironment("SourceBudgets__Odesli__WindowSeconds", "60")
                .WithEnvironment("SourceBudgets__Odesli__SafetyMarginPercent", "0");
        }

        builder.AddContainer("local-proxy", "caddy", "2.9-alpine")
            .WithEndpointProxySupport(false)
            .WithHttpEndpoint(targetPort: 80, name: "http", isProxied: false)
            .WithBindMount(
                Path.Combine(resolvedContentRootPath, "caddy", "Caddyfile"),
                "/etc/caddy/Caddyfile",
                isReadOnly: true);
    }
}
