using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Projection;
using Soundtrail.Services.Enrichment.Scheduler;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;
using Soundtrail.Services.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddFeatures<SchedulerAssemblyMarker>();
#pragma warning disable ASP0000
using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
var features = serviceProvider.GetServices<IFeature>().ToArray();

foreach (var feature in features)
{
    feature.ConfigureServices(builder.Services, builder.Configuration);
}

HandlerCollection.AddScheduledMessageHandlersFromAssemblies(
    builder.Services,
    typeof(SchedulerAssemblyMarker));

var app = builder.Build();

foreach (var feature in features.OfType<ISchedulerFeature>())
{
    feature.ConfigureApplication(app);
}

app.MapDefaultEndpoints();
await app.RunAsync();
