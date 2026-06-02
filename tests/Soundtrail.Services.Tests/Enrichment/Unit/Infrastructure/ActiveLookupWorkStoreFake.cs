using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;

namespace Soundtrail.Services.Tests.Enrichment.Unit.Infrastructure;

internal sealed class ActiveLookupWorkStoreFake : IActiveLookupWorkStore
{
    private readonly Dictionary<string, ActiveLookupWork> reservedByMusicCatalogId = [];

    public IReadOnlyCollection<ActiveLookupWork> Reservations => this.reservedByMusicCatalogId.Values.ToArray();

    public Task<bool> TryReserveAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        DateTimeOffset reservedUntil,
        CancellationToken cancellationToken)
    {
        if (this.reservedByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var existing) &&
            existing.ReservedUntil > reservedUntil.AddYears(-100))
        {
            return Task.FromResult(false);
        }

        this.reservedByMusicCatalogId[musicCatalogId.Value] = new ActiveLookupWork(musicCatalogId, commandId, reservedUntil);
        return Task.FromResult(true);
    }

    public Task ReleaseAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        CancellationToken cancellationToken)
    {
        if (this.reservedByMusicCatalogId.TryGetValue(musicCatalogId.Value, out var existing) &&
            existing.CommandId == commandId)
        {
            this.reservedByMusicCatalogId.Remove(musicCatalogId.Value);
        }

        return Task.CompletedTask;
    }
}
