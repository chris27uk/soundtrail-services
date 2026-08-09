using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Composition;

public sealed record ImportKworbChartPorts(Func<IServiceProvider, ICommandBus> CommandBus);

public static class ImportKworbChartComposition
{
    public static void Configure(IServiceCollection services, ImportKworbChartPorts ports)
    {
        services.TryAddSingleton(ports.CommandBus);
        services.TryAddScoped<IScheduledMessageHandler<ImportKworbChartCommand>, ImportKworbChartHandler>();
    }
}
