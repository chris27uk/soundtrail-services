using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddLookupMusicRequestQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddSingleton<IResolutionDemandSignalPort, WolverineResolutionDemandSignalQueue>();
        return services;
    }
}
