using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Catalog.Search.Adapters;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Composition;

[Autodiscover]
public sealed class SearchFeatureProduction() : SearchFeature(
    _ => new SystemClockPort(),
    sp => new RavenSearchPort(sp.GetRequiredService<IDocumentStore>()),
    sp => new RavenDiscoveryFeedbackPort(sp.GetRequiredService<IDocumentStore>()));

public class SearchFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, ISearchPort> createSearchPort,
    Func<IServiceProvider, IDiscoveryFeedbackPort> createDiscoveryFeedbackPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<SearchRequest, SearchResponse?>, SearchHandler>();
        services.Add(ServiceDescriptor.Singleton(createSearchPort));
        services.Add(ServiceDescriptor.Singleton(createDiscoveryFeedbackPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(WebApplication app)
    {
        app.MapSearchEndpoints(AppTypeRegistry.ServiceLocation);
    }
}
