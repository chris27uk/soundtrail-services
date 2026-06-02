using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;

public interface IActiveLookupWorkStore
{
    Task<bool> TryReserveAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        DateTimeOffset reservedUntil,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        CancellationToken cancellationToken);
}
