using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.ServiceDefaults;
using Soundtrail.Tools.Operations.Features.ImportKworbChart;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Adapters;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;
using Soundtrail.Tools.Operations.Infrastructure;
using Soundtrail.Tools.Operations.Infrastructure.CommandLine;
using Wolverine;
using Wolverine.AzureServiceBus;
using ICommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportKworbChart.CompositionRoot;

[Autodiscover]
public sealed class ImportKworbChartFeatureProduction() : ImportKworbChartFeature(
    sp => new KworbChartPort(sp.GetRequiredService<IHttpClientFactory>().CreateClient(KworbChartPort.HttpClientName)),
    sp => new RavenLoadTrackByFingerprintPort(sp.GetRequiredService<IDocumentStore>()));

public class ImportKworbChartFeature(
    Func<IServiceProvider, IReadKworbChartPort> createReadKworbChartPort,
    Func<IServiceProvider, ILoadTrackByFingerprintPort> createLoadTrackByFingerprintPort) : IOperationsFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Configure<PlaylistUpdatesServiceBusOptions>(configuration.GetSection(PlaylistUpdatesServiceBusOptions.SectionName));
        services.TryAddScoped<ICommandBus, WolverineCommandBus>();
        services.AddHttpClient(KworbChartPort.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://kworb.net", UriKind.Absolute);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Soundtrail/1.0");
        });

        services.TryAddScoped<CommandLineDispatcher>();
        services.TryAddScoped<IHandler<ImportKworbChartCommand>, ImportKworbChartHandler>();
        services.TryAddScoped<ICommandLineOptionsHandler, ImportKworbChartCommandLineHandler>();
        services.Add(ServiceDescriptor.Scoped(createReadKworbChartPort));
        services.Add(ServiceDescriptor.Scoped(createLoadTrackByFingerprintPort));
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();
        options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

        var serviceBusOptions = configuration
            .GetSection(PlaylistUpdatesServiceBusOptions.SectionName)
            .Get<PlaylistUpdatesServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
        }

        if (!useDevelopmentEmulator)
        {
            transport.AutoProvision();
        }

        options.PublishMessage<PlaylistUpdated>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaylistUpdatesQueueName);
    }
}

internal sealed class PlaylistUpdatesServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string PlaylistUpdatesQueueName { get; init; } = "playlist-updates";
}
