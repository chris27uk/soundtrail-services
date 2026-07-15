using JasperFx.CodeGeneration.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Messaging;

[Autodiscover]
public sealed class SchedulerMessagingFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.AddWolverineCommandBus();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();
        options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;
        options.Discovery.DisableConventionalDiscovery();

        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (ShouldUseLocalMessaging(serviceBusOptions.ConnectionString, environment))
        {
            options.StubAllExternalTransports();
            options.PublishMessage<RunDiscoveryBacklogSchedulingCommandDto>()
                .ToLocalQueue(serviceBusOptions.DiscoveryBacklogSchedulingQueueName);
            return;
        }

        var useDevelopmentEmulator = serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString();

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString);
        if (useDevelopmentEmulator)
        {
            transport.SystemQueuesAreEnabled(false);
        }
        else
        {
            transport.AutoProvision()
                .EnableWolverineControlQueues();
        }

        options.PublishMessage<RunDiscoveryBacklogSchedulingCommandDto>()
            .ToAzureServiceBusQueue(serviceBusOptions.DiscoveryBacklogSchedulingQueueName);
    }

    private static bool ShouldUseLocalMessaging(string? connectionString, IHostEnvironment environment)
    {
        return environment.IsDevelopment()
               && (string.IsNullOrWhiteSpace(connectionString)
                   || connectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase));
    }
}
