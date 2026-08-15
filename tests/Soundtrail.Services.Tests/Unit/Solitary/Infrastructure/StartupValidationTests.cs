using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Startup;
using Soundtrail.Services.ServiceDefaults;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure;

public class StartupValidationTests
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Given_All_Startup_Validation_Passes_When_Host_Starts_Then_Readiness_Is_Healthy()
    {
        using var host = await StartHostAsync(
            builder => builder.Services.AddStartupValidation("pass", (_, _) => Task.CompletedTask));

        var result = await CheckHealthAsync(host);

        result.Status.Should().Be(HealthStatus.Healthy);
        host.Services.GetRequiredService<StartupValidationState>().GetSnapshot().Completed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_A_Startup_Validation_Fails_When_Host_Starts_Then_Readiness_Is_Unhealthy()
    {
        using var host = await StartHostAsync(
            builder => builder.Services.AddStartupValidation(
                "failing-check",
                (_, _) => Task.FromException(new InvalidOperationException("broken configuration"))));

        var result = await CheckHealthAsync(host);
        var entry = result.Entries["startup_validation"];

        result.Status.Should().Be(HealthStatus.Unhealthy);
        entry.Description.Should().Contain("startup validation failure");
        entry.Data.Values.Should().Contain(value => value.ToString()!.Contains("broken configuration"));
    }

    [Fact]
    public async Task Given_Service_Bus_Is_Not_Configured_When_Host_Starts_Then_Readiness_Is_Unhealthy()
    {
        using var host = await StartHostAsync(builder =>
        {
            builder.Services.AddAzureServiceBusCommandBus();
        });

        var result = await CheckHealthAsync(host);
        var entry = result.Entries["startup_validation"];

        result.Status.Should().Be(HealthStatus.Unhealthy);
        entry.Data.Values.Should().Contain(value => value.ToString()!.Contains("ServiceBus:ConnectionString is not configured."));
    }

    [Fact]
    public async Task Given_Worker_Source_Budgets_Are_Missing_When_Host_Starts_Then_Readiness_Is_Unhealthy()
    {
        using var host = await StartHostAsync(
            builder =>
            {
                builder.Configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false"
                    });
                builder.Services.Configure<SourceApiBudgetsOptions>(builder.Configuration.GetSection("SourceBudgets"));
                builder.Services.AddWorkerStartupValidation(builder.Configuration);
            });

        var result = await CheckHealthAsync(host);
        var entry = result.Entries["startup_validation"];

        result.Status.Should().Be(HealthStatus.Unhealthy);
        entry.Data.Values.Should().Contain(value => value.ToString()!.Contains("SourceBudgets:MusicBrainz is not configured."));
    }

    private static async Task<HealthReport> CheckHealthAsync(IHost host)
    {
        using var timeout = new CancellationTokenSource(HealthCheckTimeout);
        return await host.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(timeout.Token);
    }

    private static async Task<IHost> StartHostAsync(Action<HostApplicationBuilder> configure)
    {
        var builder = Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings
            {
                EnvironmentName = Environments.Development
            });

        // Avoid AddServiceDefaults: OTEL + StandardResilienceHandler (10s/attempt, 30s total) on every HttpClient.
        builder.Services.AddStartupValidationInfrastructure();
        configure(builder);

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }
}
