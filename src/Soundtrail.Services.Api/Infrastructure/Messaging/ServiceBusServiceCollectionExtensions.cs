using Soundtrail.Adapters.Messaging;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogSearchAttemptQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddAzureServiceBusCommandBus();
        return services;
    }
}
