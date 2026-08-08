using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Adapters;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Composition;

[Autodiscover]
public sealed class ImportKworbChartFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IHandler<ImportKworbChartCommand>, ImportKworbChartHandler>();
        services.TryAddScoped<ImportKworbChartTickerFunctions>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
