using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportKworbChart.Ports;

public interface IReadKworbChartPort
{
    Task<IReadOnlyList<TrackReference>> ReadAsync(CancellationToken cancellationToken);
}
