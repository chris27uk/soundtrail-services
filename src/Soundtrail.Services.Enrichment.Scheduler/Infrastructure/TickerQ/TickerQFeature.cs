using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;
using TickerQ.DependencyInjection;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.EntityFrameworkCore.DbContextFactory;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Enums;
using Wolverine;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.TickerQ;

[Autodiscover]
public sealed class TickerQFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var tickerQOptions = configuration
            .GetSection(TickerQOptions.SectionName)
            .Get<TickerQOptions>() ?? new TickerQOptions();

        services.Configure<TickerQOptions>(configuration.GetSection(TickerQOptions.SectionName));
        services.AddOptions<TickerQOptions>()
            .Bind(configuration.GetSection(TickerQOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "TickerQ connection string is required.")
            .ValidateOnStart();

        services.AddTickerQ(
            tickerConfiguration =>
            {
                tickerConfiguration
                    .AddOperationalStore(
                        efConfiguration => efConfiguration.UseTickerQDbContext<TickerQDbContext>(
                            optionsAction => optionsAction.UseSqlite(tickerQOptions.ConnectionString),
                            null!))
                    .AddDashboard(
                        dashboard =>
                        {
                            dashboard.SetBasePath(tickerQOptions.Dashboard.BasePath);

                            if (string.IsNullOrWhiteSpace(tickerQOptions.Dashboard.Username) || string.IsNullOrWhiteSpace(tickerQOptions.Dashboard.Password))
                            {
                                dashboard.WithNoAuth();
                            }
                            else
                            {
                                dashboard.WithBasicAuth(tickerQOptions.Dashboard.Username, tickerQOptions.Dashboard.Password);
                            }
                        });
            });
    }

    public void ConfigureApplication(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<TickerQDbContext>();
        dbContext.Database.EnsureCreated();

        app.UseTickerQ(TickerQStartMode.Immediate);
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
    }
}
