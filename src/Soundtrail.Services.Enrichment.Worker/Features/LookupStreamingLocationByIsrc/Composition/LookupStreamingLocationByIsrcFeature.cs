using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.ServiceDefaults;
using StackExchange.Redis;
using Wolverine;
using Wolverine.AzureServiceBus;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using DomainCommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByIsrc.Composition;

[Autodiscover]
public sealed class LookupStreamingLocationByIsrcFeature : IFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWolverineCommandBus();
        services.AddWorkerRavenDocumentStore(configuration);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<RedisLookupExecutionAdmissionOptions>(configuration.GetSection(RedisLookupExecutionAdmissionOptions.SectionName));
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection("SourceBudgets"));
        services.Configure<OdesliOptions>(configuration.GetSection(OdesliOptions.SectionName));
        services.AddHttpClient(OdesliStreamingLocationPort.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OdesliOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Soundtrail/1.0");
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is required.")));
        services.TryAddSingleton<IClockPort, SystemClockPort>();
        services.AddLookupHandlerPipeline<LookupStreamingLocationByIsrcMessage, LookupStreamingLocationByIsrcDecoratorMetadata>(
            sp => new LookupStreamingLocationByIsrcHandler(
                sp.GetRequiredService<IReadTrackForLookupPort>(),
                sp.GetRequiredService<IReadStreamingLocationByProviderPort>(),
                sp.GetRequiredService<IClockPort>(),
                sp.GetRequiredService<DomainCommandBus>()));
        services.TryAddScoped<IReadStreamingLocationByProviderPort>(
            sp => new OdesliStreamingLocationPort(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(OdesliStreamingLocationPort.HttpClientName),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OdesliOptions>>()));
        services.TryAddScoped<IReadTrackForLookupPort, RavenReadTrackForLookupPort>();
        services.TryAddScoped<ILookupExecutionAdmissionPort, RedisLookupExecutionAdmissionPort>();
        services.TryAddScoped<ILookupExecutionReceiptStore, RavenLookupExecutionReceiptStore>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
            return;
        }

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (!serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString())
        {
            transport.AutoProvision();
        }

        options.ListenToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName)
            .ProcessInline();

        options.PublishMessage<CatalogLookupCompleted>()
            .ToAzureServiceBusQueue(serviceBusOptions.CatalogLookupCompletedQueueName);
    }
}
