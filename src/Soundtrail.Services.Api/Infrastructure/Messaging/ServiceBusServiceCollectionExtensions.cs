using JasperFx.CodeGeneration.Model;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Services.Api.Features.SearchCatalog.Ports;
using Soundtrail.Services.ServiceDefaults;
using Wolverine;
using Wolverine.AzureServiceBus;

namespace Soundtrail.Services.Api.Infrastructure.Messaging;

public static class ServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogSearchAttemptQueue(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.TryAddScoped<IEnqueueCatalogSearchAttempt, WolverineEnqueueCatalogSearchAttempt>();
        services.TryAddScoped<IQueueCatalogSearchAttempt>(sp => sp.GetRequiredService<IEnqueueCatalogSearchAttempt>());
        services.TryAddScoped<Soundtrail.Services.Api.Features.SearchCatalog.Ports.IQueueCatalogSearchAttemptPort>(sp => sp.GetRequiredService<IQueueCatalogSearchAttempt>());
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

        if (environment.IsEnvironment("Testing"))
        {
            opts.StubAllExternalTransports();
        }

        if (!useDevelopmentEmulator)
        {
            transport.AutoProvision();
        }

        opts.PublishMessage<CatalogSearchAttemptDto>()
            .ToAzureServiceBusQueue(options.CatalogSearchAttemptsQueueName);

        return opts;
    }
}
