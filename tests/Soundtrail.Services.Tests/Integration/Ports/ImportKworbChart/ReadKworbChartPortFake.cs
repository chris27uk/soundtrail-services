using Soundtrail.Domain.Catalog;
using Soundtrail.Tools.Operations.Features.ImportKworbChart.Ports;

namespace Soundtrail.Services.Tests.Integration.Ports.ImportKworbChart;

internal sealed class ReadKworbChartPortFake(IReadOnlyList<TrackReference>? rows = null) : IReadKworbChartPort
{
    public Task<IReadOnlyList<TrackReference>> ReadAsync(CancellationToken cancellationToken) =>
        Task.FromResult(rows ?? Array.Empty<TrackReference>());
}
