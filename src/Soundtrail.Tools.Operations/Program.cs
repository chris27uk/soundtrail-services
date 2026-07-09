using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Tools.Operations;
using Soundtrail.Tools.Operations.Infrastructure.CommandLine;
using Soundtrail.Tools.Operations.Infrastructure;
using Wolverine;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

using (var _ = FeatureEnvironment.Live())
{
    builder.Services.AddFeatures<OperationsAssemblyMarker>();
#pragma warning disable ASP0000
    using var serviceProvider = builder.Services.BuildServiceProvider();
#pragma warning restore ASP0000
    var features = serviceProvider.GetServices<IFeature>().ToArray();

    foreach (var feature in features)
    {
        feature.ConfigureServices(builder.Services, builder.Configuration);
    }

    builder.Services.AddWolverine(opts =>
    {
        foreach (var feature in features.OfType<IOperationsFeature>())
        {
            feature.ConfigureMessaging(opts, builder.Configuration, builder.Environment);
        }
    });

    using var host = builder.Build();
    await host.StartAsync();

    var dispatcher = host.Services.GetRequiredService<CommandLineDispatcher>();
    var exitCode = await dispatcher.DispatchAsync(args, CancellationToken.None);

    await host.StopAsync();
    return exitCode;
}
