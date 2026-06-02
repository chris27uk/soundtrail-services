using Raven.Client.Documents;
using Soundtrail.Services.Enrichment.Features.Scheduling.Contracts;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

public sealed class RavenActiveLookupWorkStore(IDocumentStore documentStore) : IActiveLookupWorkStore
{
    public async Task<bool> TryReserveAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        DateTimeOffset reservedUntil,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;

        var documentId = RavenActiveLookupWorkDocument.GetDocumentId(musicCatalogId.Value);
        var existing = await session.LoadAsync<RavenActiveLookupWorkDocument>(documentId, cancellationToken);

        if (existing is not null && existing.ReservedUntil > DateTimeOffset.UtcNow)
        {
            return false;
        }

        var reservation = new RavenActiveLookupWorkDocument
        {
            Id = documentId,
            MusicCatalogId = musicCatalogId.Value,
            CommandId = commandId,
            ReservedUntil = reservedUntil
        };

        await session.StoreAsync(reservation, cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ReleaseAsync(
        MusicCatalogId musicCatalogId,
        string commandId,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var documentId = RavenActiveLookupWorkDocument.GetDocumentId(musicCatalogId.Value);
        var existing = await session.LoadAsync<RavenActiveLookupWorkDocument>(documentId, cancellationToken);
        if (existing is null || existing.CommandId != commandId)
        {
            return;
        }

        session.Delete(existing);
        await session.SaveChangesAsync(cancellationToken);
    }
}
