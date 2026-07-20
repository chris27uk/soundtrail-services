using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

[Autodiscover]
public sealed class ProjectorMessagingFeature : IProjectorFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
            return;
        }

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (!serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString())
        {
            transport.AutoProvision();
        }

        options.PublishMessage<AssessWorkMessage>()
            .ToAzureServiceBusQueue(serviceBusOptions.AssessMusicCatalogItemQueueName);

        options.PublishMessage<DispatchLookupWork>()
            .ToAzureServiceBusQueue(serviceBusOptions.DispatchLookupWorkQueueName);

        options.PublishMessage<PlaylistUpdated>()
            .ToAzureServiceBusQueue(serviceBusOptions.PlaylistUpdatesQueueName);
    }
}
