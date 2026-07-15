using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Ports;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Wolverine;
using Wolverine.AzureServiceBus;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Composition;

[Autodiscover]
public sealed class ImportKworbChartFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddHttpClient(KworbChartPort.HttpClientName, client =>
        {
            client.BaseAddress = new Uri("https://kworb.net", UriKind.Absolute);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Soundtrail/1.0");
        });

        services.TryAddScoped<IHandler<ImportKworbChartCommand>, ImportKworbChartHandler>();
        services.TryAddScoped<ImportKworbChartTickerFunctions>();
        services.AddScoped<IReadKworbChartPort>(
            sp => new KworbChartPort(sp.GetRequiredService<IHttpClientFactory>().CreateClient(KworbChartPort.HttpClientName)));
        services.AddScoped<ILoadTrackByFingerprintPort, RavenLoadTrackByFingerprintPort>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (ShouldUseLocalMessaging(serviceBusOptions.ConnectionString, environment))
        {
            options.StubAllExternalTransports();
            options.PublishMessage<PlaylistUpdated>()
                .ToLocalQueue(serviceBusOptions.PlaylistUpdatesQueueName);
            return;
        }

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
        }

        options.PublishMessage<PlaylistUpdated>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaylistUpdatesQueueName);
    }

    private static bool ShouldUseLocalMessaging(string? connectionString, IHostEnvironment environment)
    {
        return environment.IsDevelopment()
               && (string.IsNullOrWhiteSpace(connectionString)
                   || connectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase));
    }
}
