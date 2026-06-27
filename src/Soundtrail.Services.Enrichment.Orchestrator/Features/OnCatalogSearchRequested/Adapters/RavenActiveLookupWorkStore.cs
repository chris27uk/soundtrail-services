using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters.Documents;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class RavenActiveLookupWorkStore(
    IDocumentStore documentStore,
    IAsyncDocumentSession? session = null) : IActiveLookupWorkStore
{
    public async Task<bool> TryAcquireAsync(
        CommandId commandId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            activeSession.Advanced.UseOptimisticConcurrency = true;

            var documentId = RavenActiveLookupWorkRecordDto.GetDocumentId(commandId.Value);
            var existing = await activeSession.LoadAsync<RavenActiveLookupWorkRecordDto>(documentId, cancellationToken);

            if (existing is not null && existing.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return false;
            }

            var activeLock = new RavenActiveLookupWorkRecordDto
            {
                Id = documentId,
                CommandId = commandId.Value,
                ExpiresAt = expiresAt
            };

            await activeSession.StoreAsync(activeLock, cancellationToken);
            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }

    public async Task ReleaseAsync(
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var ownsSession = session is null;
        var (activeSession, dispose) = OpenSession();
        using (dispose)
        {
            var documentId = RavenActiveLookupWorkRecordDto.GetDocumentId(commandId.Value);
            var existing = await activeSession.LoadAsync<RavenActiveLookupWorkRecordDto>(documentId, cancellationToken);
            if (existing is null || existing.CommandId != commandId.Value)
            {
                return;
            }

            activeSession.Delete(existing);
            if (ownsSession)
            {
                await activeSession.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private (IAsyncDocumentSession Session, IDisposable Dispose) OpenSession()
    {
        if (session is not null)
        {
            return (session, NoopDisposable.Instance);
        }

        var openedSession = documentStore.OpenAsyncSession();
        return (openedSession, openedSession);
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
