using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.Search.Adapters;
using Soundtrail.Services.Api.Features.Search.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.Search.Composition;

[Autodiscover]
public sealed class SearchFeatureProduction() : SearchFeature(
    _ => new SystemClockPort(),
    sp => new RavenSearchPort(CreateDocumentStore(sp), AppTypeRegistry.ServiceLocation))
{
    private static IDocumentStore CreateDocumentStore(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<RavenDbOptions>>().Value;
        var store = new DocumentStore
        {
            Urls = options.Urls,
            Database = options.Database,
            Conventions = new DocumentConventions
            {
                FindCollectionName = type => type.Name
            }
        };

        return store.Initialize();
    }
}

public class SearchFeature(
    Func<IServiceProvider, IClockPort> createClockPort,
    Func<IServiceProvider, ISearchPort> createSearchPort) : IApiFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
        services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
        services.TryAddScoped<IApiHandler<SearchRequest, SearchResponse?>, SearchHandler>();
        services.Add(ServiceDescriptor.Singleton(createSearchPort));
        services.Add(ServiceDescriptor.Singleton(createClockPort));
    }

    public void ConfigureApplication(IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints => endpoints.MapSearchEndpoints(AppTypeRegistry.ServiceLocation));
    }
}
