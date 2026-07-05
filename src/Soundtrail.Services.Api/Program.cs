using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Api;
using Soundtrail.Services.Api.Infrastructure;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseWolverine(opts => opts.UseApiServiceBusMessaging(builder.Configuration, builder.Environment));

using (var _ = FeatureEnvironment.Live())
{
    builder.Services.AddFeatures<ApiAssemblyMarker>();
#pragma warning disable ASP0000
    using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
    var features = serviceProvider.GetServices<IFeature>().ToArray();

    foreach (var initializer in features)
    {
        initializer.ConfigureServices(builder.Services, builder.Configuration);
    }

    var app = builder.Build();

    foreach (var initializer in features.OfType<IApiFeature>())
    {
        initializer.ConfigureApplication(app);
    }
    
    app.MapDefaultEndpoints();
    app.Run();
}
