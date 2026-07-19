using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Internal.Projector;
using Soundtrail.Services.Internal.Projector.Infrastructure;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddFeatures<ProjectorAssemblyMarker>();
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
        foreach (var feature in features.OfType<IProjectorFeature>())
        {
            feature.ConfigureMessaging(options, builder.Configuration, builder.Environment);
        }
    });

var app = builder.Build();

foreach (var feature in features.OfType<IProjectorFeature>())
{
    feature.ConfigureApplication(app);
}

app.MapDefaultEndpoints();
await app.RunAsync();
