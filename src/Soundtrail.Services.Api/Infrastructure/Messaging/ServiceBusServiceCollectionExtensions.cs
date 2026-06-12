using Soundtrail.Services.Api.Features.Search.Queueing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddLookupMusicRequestQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.TryAddScoped<IEnqueueMusicRequest, WolverineEnqueueMusicRequest>();
        return services;
    }
}
