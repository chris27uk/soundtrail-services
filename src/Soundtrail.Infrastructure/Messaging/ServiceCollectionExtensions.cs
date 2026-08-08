using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Adapters.Messaging;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusCommandBus(this IServiceCollection services)
    {
        services.TryAddSingleton(
            sp => new AzureServiceBusMessageProcessingOptions
            {
                ConnectionString = sp.GetRequiredService<IConfiguration>()[$"ServiceBus:ConnectionString"] ?? string.Empty,
                Enabled = !sp.GetRequiredService<IHostEnvironment>().IsEnvironment("Testing")
                          && !string.IsNullOrWhiteSpace(sp.GetRequiredService<IConfiguration>()[$"ServiceBus:ConnectionString"])
                          && !(sp.GetRequiredService<IConfiguration>()[$"ServiceBus:ConnectionString"]?.Contains("replace-me", StringComparison.OrdinalIgnoreCase) ?? false)
            });
        services.TryAddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        services.TryAddSingleton<AzureServiceBusMessageTransport>();
        services.TryAddScoped<AzureServiceBusCommandBus>();
        services.TryAddScoped<Soundtrail.Domain.Abstractions.ICommandBus, AzureServiceBusCommandBus>();
        services.AddStartupValidation(
            "azure-service-bus",
            (serviceProvider, cancellationToken) =>
            {
                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsEnvironment("Testing"))
                {
                    return Task.CompletedTask;
                }

                var options = serviceProvider.GetRequiredService<AzureServiceBusMessageProcessingOptions>();
                if (string.IsNullOrWhiteSpace(options.ConnectionString)
                    || options.ConnectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("ServiceBus:ConnectionString is not configured.");
                }

                _ = serviceProvider.GetRequiredService<AzureServiceBusMessageTransport>();

                using var scope = serviceProvider.CreateScope();
                _ = scope.ServiceProvider.GetRequiredService<Soundtrail.Domain.Abstractions.ICommandBus>();
                return Task.CompletedTask;
            });
        return services;
    }
}
