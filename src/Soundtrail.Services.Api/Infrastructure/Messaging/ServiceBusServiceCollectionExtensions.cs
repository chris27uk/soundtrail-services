using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddLookupMusicRequestQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        return services;
    }
}
