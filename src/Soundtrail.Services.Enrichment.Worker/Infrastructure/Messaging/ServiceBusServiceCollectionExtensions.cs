using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServiceBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddSingleton<ILookupMusicCommandQueue, WolverineLookupMusicCommandQueue>();
        return services;
    }
}
