using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Soundtrail.Services.Api.Features.Search.Queueing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;

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

    public static WolverineOptions UseApiServiceBusMessaging(
        this WolverineOptions opts,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        opts.UseRuntimeCompilation();
        opts.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

        var options = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");
        var useDevelopmentEmulator = options.ConnectionString.IsDevelopmentEmulatorConnectionString();

        var transport = opts.UseAzureServiceBus(options.ConnectionString)
            .SystemQueuesAreEnabled(false);

        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            opts.StubAllExternalTransports();
        }

        if (!useDevelopmentEmulator)
        {
            transport.AutoProvision();
        }

        opts.PublishMessage<LookupMusicRequestDto>()
            .ToAzureServiceBusQueue(options.LookupMusicRequestsQueueName);

        return opts;
    }
}
