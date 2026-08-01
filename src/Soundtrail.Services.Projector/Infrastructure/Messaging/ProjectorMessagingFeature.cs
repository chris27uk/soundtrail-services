using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.ServiceDefaults;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

[Autodiscover]
public sealed class ProjectorMessagingFeature : IProjectorFeature
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
