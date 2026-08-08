using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Composition;

[Autodiscover]
public sealed class SearchFeatureProduction : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.AddAzureServiceBusCommandBus();

        SearchComposition.Configure(services, new(
            sp => new RavenSearchPort(sp.GetRequiredService<IDocumentStore>()),
            _ => new SystemClockPort(),
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => new RavenDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>())));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapSearchEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
