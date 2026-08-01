using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.TypeRegistry;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

[Autodiscover]
public sealed class ProjectionSubscriptionsFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.TryAddScoped<StoredEventDomainEventResolver>();
        services.TryAddScoped<CatalogProjectionDispatcher>();
        services.TryAddScoped<DiscoveryProjectionDispatcher>();
        services.AddHostedService<CatalogProjectionSubscriptionService>();
        services.AddHostedService<DiscoveryProjectionSubscriptionService>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
