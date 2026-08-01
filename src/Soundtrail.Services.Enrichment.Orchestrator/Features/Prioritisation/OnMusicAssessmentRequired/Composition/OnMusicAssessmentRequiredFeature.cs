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
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Messaging;
using Soundtrail.Services.ServiceDefaults;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Prioritisation.OnMusicAssessmentRequired.Composition;

[Autodiscover]
public sealed class OnMusicAssessmentRequiredFeature : IOrchestratorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddAzureServiceBusListener<AssessMusicCatalogItemCommandDto, AssessWorkMessage>(
            "assess-music-catalog-item");
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<PlanningAssessmentOptions>(configuration.GetSection(PlanningAssessmentOptions.SectionName));
        services.TryAddSingleton<IPlanningAssessmentPolicy, PlanningAssessmentPolicy>();
        services.TryAddScoped<IDiscoveryPlanningProjectionReader, RavenDiscoveryPlanningProjectionReader>();
        services.TryAddScoped<IEventStreamRepository<CatalogWorkId>, CatalogSearchEventStreamRepository>();
        services.TryAddScoped<OnMusicAssessmentRequiredHandler>();
        services.TryAddScoped<IHandler<AssessWorkMessage>, OnMusicAssessmentRequiredHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
