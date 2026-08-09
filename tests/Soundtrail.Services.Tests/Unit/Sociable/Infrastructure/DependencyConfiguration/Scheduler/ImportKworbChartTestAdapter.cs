using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Composition;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Scheduler;

internal sealed class ImportKworbChartTestAdapter(ImportKworbChartPorts ports) : ISociableFeature
{
    public static ImportKworbChartTestAdapter Default() => new(DefaultPorts());

    public static ImportKworbChartTestAdapter With(
        Func<ImportKworbChartPorts, ImportKworbChartPorts> customize) =>
        new(customize(DefaultPorts()));

    public static ImportKworbChartPorts DefaultPorts() =>
        new(sp => sp.GetRequiredService<ICommandBus>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        ImportKworbChartComposition.Configure(services, ports);
}
