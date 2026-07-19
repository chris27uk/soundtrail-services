using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.EventSourcing.CompositionRoot;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.RequestedWork;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.IncomingWork.OnKnownMusicDataRequested.Composition;

[Autodiscover]
public sealed class OnKnownMusicDataRequestedFeature : IOrchestratorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.TryAddSingleton<IWorkPlanner, WorkPlanner>();
        services.TryAddScoped<IHandler<RequestKnownMusicDataCommand>, OnKnownMusicDataRequestedHandler>();
        services.AddCatalogSearchEventStreamRepository();
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

        options.ListenToAzureServiceBusQueue(serviceBusOptions.KnownMusicDataRequestsQueueName)
            .ProcessInline();

        options.PublishMessage<RequestKnownMusicDataCommand>()
            .ToAzureServiceBusQueue(serviceBusOptions.KnownMusicDataRequestsQueueName);
    }
}
