using Soundtrail.Services.Enrichment.Models;

namespace Soundtrail.Services.Enrichment.Ports;

public interface ISearchIndexPort
{
    Task UpsertAsync(
        TrackMapping mapping,
        CancellationToken cancellationToken);
}
