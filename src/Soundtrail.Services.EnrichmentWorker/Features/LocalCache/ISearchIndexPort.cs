using Soundtrail.Services.EnrichmentWorker.Models;

namespace Soundtrail.Services.EnrichmentWorker.Ports;

public interface ISearchIndexPort
{
    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
