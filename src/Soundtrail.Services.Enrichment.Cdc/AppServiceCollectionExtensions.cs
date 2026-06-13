using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Cdc.Features;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Raven;

namespace Soundtrail.Services.Enrichment.Cdc;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddCdcAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCdcServiceBus(configuration);
        services.AddCdcRavenDocumentStore(configuration);
        services.AddCdcFeature();
        return services;
    }
}
