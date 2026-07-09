using Soundtrail.Domain.Catalog;

namespace Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

public interface IReadKworbChartPort
{
    Task<IReadOnlyList<TrackReference>> ReadAsync(CancellationToken cancellationToken);
}
