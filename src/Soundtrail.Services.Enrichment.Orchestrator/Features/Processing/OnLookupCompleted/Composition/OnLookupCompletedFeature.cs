using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Composition;

[Autodiscover]
public sealed class OnLookupCompletedFeature : IOrchestratorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddAzureServiceBusListener<CatalogLookupCompletedCommandDto, CatalogLookupCompleted>(
            "catalog-lookup-completed");
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.TryAddScoped<LookupCompletedHandler>();
        services.TryAddScoped<IEventStreamRepository<CatalogWorkId>, CatalogSearchEventStreamRepository>();
        services.TryAddScoped<IHandler<CatalogLookupCompleted>, LookupCompletedHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
