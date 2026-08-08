using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddCatalogSearchAttemptQueue(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddFeatures<ApiAssemblyMarker>();
#pragma warning disable ASP0000
using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
var features = serviceProvider.GetServices<IFeature>().ToArray();

foreach (var initializer in features)
{
    initializer.ConfigureServices(builder.Services, builder.Configuration);
}

builder.Services.AddOpenApi();
var app = builder.Build();

foreach (var initializer in features.OfType<IApiFeature>())
{
    initializer.ConfigureApplication(app);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.Run();
