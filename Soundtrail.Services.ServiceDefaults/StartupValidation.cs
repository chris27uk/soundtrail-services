using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Soundtrail.Services.ServiceDefaults;

public static class StartupValidationServiceCollectionExtensions
{
    public static IServiceCollection AddStartupValidationInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<StartupValidationState>();
        services.AddHealthChecks().AddCheck<StartupValidationHealthCheck>("startup_validation");
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, StartupValidationHostedService>());
        return services;
    }

    public static IServiceCollection AddStartupValidation(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, CancellationToken, Task> validateAsync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(validateAsync);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupValidation>(
            new DelegateStartupValidation(name, validateAsync)));

        return services;
    }
}

public interface IStartupValidation
{
    string Name { get; }

    Task ValidateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

public sealed record StartupValidationFailure(string Name, string Message);

public sealed record StartupValidationSnapshot(bool Completed, IReadOnlyList<StartupValidationFailure> Failures);

public sealed class StartupValidationState
{
    private readonly Lock sync = new();
    private readonly List<StartupValidationFailure> failures = [];

    public StartupValidationSnapshot GetSnapshot()
    {
        lock (sync)
        {
            return new StartupValidationSnapshot(completed, failures.ToArray());
        }
    }

    public void MarkCompleted()
    {
        lock (sync)
        {
            completed = true;
        }
    }

    public void RecordFailure(string name, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        lock (sync)
        {
            failures.Add(new StartupValidationFailure(name, message));
        }
    }

    private bool completed;
}

public sealed class StartupValidationHealthCheck(StartupValidationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var snapshot = state.GetSnapshot();
        if (!snapshot.Completed)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Startup validation has not completed."));
        }

        if (snapshot.Failures.Count == 0)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Startup validation passed."));
        }

        var data = snapshot.Failures
            .Select((failure, index) => new KeyValuePair<string, object>(
                $"failure:{index + 1}",
                $"{failure.Name}: {failure.Message}"))
            .ToDictionary();

        return Task.FromResult(HealthCheckResult.Unhealthy(
            description: $"{snapshot.Failures.Count} startup validation failure(s) detected.",
            data: data));
    }
}

internal sealed class StartupValidationHostedService(
    IServiceProvider serviceProvider,
    IEnumerable<IStartupValidation> validations,
    StartupValidationState state,
    ILogger<StartupValidationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var validation in validations)
        {
            try
            {
                await validation.ValidateAsync(serviceProvider, cancellationToken);
                logger.LogInformation("Startup validation '{ValidationName}' passed.", validation.Name);
            }
            catch (Exception exception)
            {
                state.RecordFailure(validation.Name, exception.Message);
                logger.LogError(exception, "Startup validation '{ValidationName}' failed.", validation.Name);
            }
        }

        state.MarkCompleted();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class DelegateStartupValidation(
    string name,
    Func<IServiceProvider, CancellationToken, Task> validateAsync) : IStartupValidation
{
    public string Name => name;

    public Task ValidateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        validateAsync(serviceProvider, cancellationToken);
}
