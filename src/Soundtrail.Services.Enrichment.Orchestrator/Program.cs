using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Enrichment.Orchestrator;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddFeatures<OrchestratorAssemblyMarker>();
#pragma warning disable ASP0000
using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
var features = serviceProvider.GetServices<IFeature>().ToArray();

foreach (var feature in features)
{
    feature.ConfigureServices(builder.Services, builder.Configuration);
}

builder.Host.UseWolverine(
    options =>
    {
        foreach (var feature in features.OfType<IOrchestratorFeature>())
        {
            feature.ConfigureMessaging(options, builder.Configuration, builder.Environment);
        }
    });

var app = builder.Build();

foreach (var feature in features.OfType<IOrchestratorFeature>())
{
    feature.ConfigureApplication(app);
}

app.MapDefaultEndpoints();
await app.RunAsync();
