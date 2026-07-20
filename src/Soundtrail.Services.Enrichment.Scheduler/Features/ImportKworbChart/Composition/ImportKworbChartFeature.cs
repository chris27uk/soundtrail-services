using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;
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
        services.TryAddScoped<IHandler<ImportKworbChartCommand>, ImportKworbChartHandler>();
        services.TryAddScoped<ImportKworbChartTickerFunctions>();
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
            options.PublishMessage<RequestKnownMusicDataMessage>()
                .ToLocalQueue(serviceBusOptions.KnownMusicDataRequestsQueueName);
            return;
        }

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
        }

        options.PublishMessage<RequestKnownMusicDataMessage>()
            .ToAzureServiceBusQueue(serviceBusOptions.KnownMusicDataRequestsQueueName);
    }

    private static bool ShouldUseLocalMessaging(string? connectionString, IHostEnvironment environment)
    {
        return environment.IsDevelopment()
               && (string.IsNullOrWhiteSpace(connectionString)
                   || connectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase));
    }
}
