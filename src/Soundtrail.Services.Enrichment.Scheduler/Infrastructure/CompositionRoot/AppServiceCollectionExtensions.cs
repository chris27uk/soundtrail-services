using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Hosting;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSchedulerServiceBus(configuration);
        services.Configure<SchedulerOptions>(
            configuration.GetSection(SchedulerOptions.SectionName));
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(SchedulerHostedService))
            .AddClasses(classes => classes.AssignableTo<ISchedulerHandler>())
            .AsImplementedInterfaces()
            .WithScopedLifetime());
        services.AddHostedService<SchedulerHostedService>();
        return services;
    }
}
