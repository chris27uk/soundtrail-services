using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Scheduling;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSchedulerServiceBus(configuration);
        services.Configure<DiscoveryBacklogSchedulingOptions>(
            configuration.GetSection(DiscoveryBacklogSchedulingOptions.SectionName));
        services.AddHostedService<DiscoveryBacklogSchedulingHostedService>();
        return services;
    }
}
