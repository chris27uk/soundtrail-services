using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Api.Features.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.GetAlbum.Contract;
using Soundtrail.Services.Api.Infrastructure;

namespace Soundtrail.Services.Api.Features.GetAlbum.Composition
{
    // Only the ports are injected here, everything else is shared between app and tests
    [Autodiscover]
    public class GetAlbumFeatureProduction() : GetAlbumFeature(
        _ => new SystemClockPort(),
        sp => new RavenGetAlbumPort(CreateDocumentStore(sp), AppTypeRegistry.ServiceLocation))
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

    public class GetAlbumFeature(Func<IServiceProvider, IClockPort> createClockPort, Func<IServiceProvider, IGetAlbumPort> createGetAlbumPort) : IApiFeature 
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
            services.Add(ServiceDescriptor.Singleton(AppTypeRegistry.ServiceLocation));
            services.TryAddScoped<IApiHandler<GetAlbumRequest, GetAlbumResponse?>, GetAlbumHandler>();
            services.Add(ServiceDescriptor.Singleton(createGetAlbumPort));
            services.Add(ServiceDescriptor.Singleton(createClockPort));
        }

        public void ConfigureApplication(IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints => endpoints.MapGetAlbumEndpoints(AppTypeRegistry.ServiceLocation));
        }
    }
}
