using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddAzureServiceBusMessageProcessing(builder.Configuration, builder.Environment);
builder.Services.AddWorkerStartupValidation(builder.Configuration);

builder.Services.AddFeatures<Soundtrail.Services.Enrichment.Worker.WorkerAssemblyMarker>();
#pragma warning disable ASP0000
using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
var features = serviceProvider.GetServices<IFeature>().ToArray();

foreach (var initializer in features)
{
    initializer.ConfigureServices(builder.Services, builder.Configuration);
}

var app = builder.Build();
app.MapDefaultEndpoints();
await app.RunAsync();
