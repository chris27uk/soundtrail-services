using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

[Autodiscover]
public sealed class SchedulerMessagingFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddAzureServiceBusCommandBus();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
