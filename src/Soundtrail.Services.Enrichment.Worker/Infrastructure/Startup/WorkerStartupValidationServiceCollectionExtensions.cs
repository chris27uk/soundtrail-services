using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.ServiceDefaults;
using StackExchange.Redis;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Startup;

public static class WorkerStartupValidationServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerStartupValidation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddStartupValidation(
            "worker-lookup-configuration",
            (serviceProvider, cancellationToken) =>
            {
                var redisConnectionString = configuration.GetConnectionString("Redis");
                if (string.IsNullOrWhiteSpace(redisConnectionString))
                {
                    throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");
                }

                var budgets = serviceProvider.GetRequiredService<IOptions<SourceApiBudgetsOptions>>().Value;
                ValidateBudget("MusicBrainz", budgets.MusicBrainz);
                ValidateBudget("Odesli", budgets.Odesli);
                ValidateBudget("Kworb", budgets.Kworb);

                var connectionMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
                _ = connectionMultiplexer?.IsConnected;

                return Task.CompletedTask;
            });

        return services;
    }

    private static void ValidateBudget(string providerName, ApiBudgetPolicy? policy)
    {
        if (policy is null)
        {
            throw new InvalidOperationException($"SourceBudgets:{providerName} is not configured.");
        }

        if (policy.MaxRequests <= 0)
        {
            throw new InvalidOperationException($"SourceBudgets:{providerName}:MaxRequests must be greater than zero.");
        }

        if (policy.WindowSeconds <= 0)
        {
            throw new InvalidOperationException($"SourceBudgets:{providerName}:WindowSeconds must be greater than zero.");
        }

        if (policy.MinimumSpacingSeconds < 0)
        {
            throw new InvalidOperationException($"SourceBudgets:{providerName}:MinimumSpacingSeconds cannot be negative.");
        }

        if (policy.SafetyMarginPercent < 0)
        {
            throw new InvalidOperationException($"SourceBudgets:{providerName}:SafetyMarginPercent cannot be negative.");
        }
    }
}
