using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Services.Enrichment.Cdc.Infrastructure.Cdc;

namespace Soundtrail.Services.Enrichment.Cdc.Features;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCdcFeature(this IServiceCollection services)
    {
        services.AddHostedService<MusicTrackEventSubscriptionHostedService>();
        return services;
    }
}
