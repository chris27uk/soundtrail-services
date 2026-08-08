using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
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

        OnMusicAssessmentRequiredComposition.Configure(services, new(
            sp => sp.GetRequiredService<PlanningAssessmentPolicy>(),
            sp => new RavenDiscoveryPlanningProjectionReader(sp.GetRequiredService<IDocumentStore>()),
            sp => sp.GetRequiredService<CatalogSearchEventStreamRepository>()));

        services.TryAddSingleton<PlanningAssessmentPolicy>();
        services.TryAddScoped<CatalogSearchEventStreamRepository>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
