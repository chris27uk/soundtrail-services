var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddConnectionString("servicebus");

var ravenDb = builder.AddContainer("ravendb", "ravendb/ravendb", "7.1-ubuntu-latest")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("RAVEN_Setup_Mode", "None")
    .WithEnvironment("RAVEN_Security_UnsecuredAccessAllowed", "PublicNetwork")
    .WithEnvironment("RAVEN_License_Eula_Accepted", "true");

var api = builder.AddProject<Projects.Soundtrail_Services_Api>("soundtrail-services-api")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
    .WithEnvironment("RavenDb__Database", "soundtrail");

var cdc = builder.AddProject<Projects.Soundtrail_Services_Enrichment_Cdc>("soundtrail-services-enrichment-cdc")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
    .WithEnvironment("RavenDb__Database", "soundtrail");

var discoveryPlanner = builder.AddProject<Projects.Soundtrail_Services_Enrichment_DiscoveryPlanner>("soundtrail-services-enrichment-discoveryplanner")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
    .WithEnvironment("RavenDb__Database", "soundtrail");

var musicTrackLookupCoordinator = builder.AddProject<Projects.Soundtrail_Services_Enrichment_MusicTrackLookupCoordinator>("soundtrail-services-enrichment-musictracklookupcoordinator")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus);

var worker = builder.AddProject<Projects.Soundtrail_Services_Enrichment_Worker>("soundtrail-services-enrichment-worker")
    .WithReference(serviceBus)
    .WithEnvironment("ServiceBus__ConnectionString", serviceBus)
    .WithEnvironment("RavenDb__Urls__0", ravenDb.GetEndpoint("http"))
    .WithEnvironment("RavenDb__Database", "soundtrail");

builder.Build().Run();
