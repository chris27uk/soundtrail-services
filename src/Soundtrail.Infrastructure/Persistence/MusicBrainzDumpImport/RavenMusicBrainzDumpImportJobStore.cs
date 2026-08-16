using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;

public sealed class RavenMusicBrainzDumpImportJobStore(IDocumentStore documentStore) : IMusicBrainzDumpImportJobStore
{
    private const int ConcurrencyAttempts = 5;

    public async Task<MusicBrainzDumpImportJob> EnsureAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);

        ConcurrencyException? lastConflict = null;
        for (var attempt = 0; attempt < ConcurrencyAttempts; attempt++)
        {
            try
            {
                return await EnsureOnceAsync(jobId, dumpVersion, requestedAt, cancellationToken);
            }
            catch (ConcurrencyException exception) when (attempt < ConcurrencyAttempts - 1)
            {
                lastConflict = exception;
            }
        }

        throw new InvalidOperationException(
            $"MusicBrainz dump job '{jobId.Value}' was modified concurrently while ensuring; retry the import trigger.",
            lastConflict);
    }

    private async Task<MusicBrainzDumpImportJob> EnsureOnceAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;
        var documentId = MusicBrainzDumpImportJobDocument.DocumentId(jobId);
        var existing = await session.LoadAsync<MusicBrainzDumpImportJobDocument>(documentId, cancellationToken);
        if (existing is null)
        {
            var created = MusicBrainzDumpImportJob.CreateNew(jobId, dumpVersion, requestedAt);
            await session.StoreAsync(
                MusicBrainzDumpImportJobDocument.FromDomain(created),
                documentId,
                cancellationToken);
            await session.SaveChangesAsync(cancellationToken);
            return created;
        }

        var job = existing.ToDomain();
        job.PrepareForRetrigger(requestedAt);
        var changeVector = session.Advanced.GetChangeVectorFor(existing);
        session.Advanced.Evict(existing);
        await session.StoreAsync(
            MusicBrainzDumpImportJobDocument.FromDomain(job),
            changeVector,
            documentId,
            cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task<MusicBrainzDumpImportJob?> GetAsync(
        MusicBrainzDumpImportJobId jobId,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        var document = await session.LoadAsync<MusicBrainzDumpImportJobDocument>(
            MusicBrainzDumpImportJobDocument.DocumentId(jobId),
            cancellationToken);
        return document?.ToDomain();
    }

    public async Task SaveAsync(
        MusicBrainzDumpImportJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        using var session = documentStore.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;
        var documentId = MusicBrainzDumpImportJobDocument.DocumentId(job.Id);
        var existing = await session.LoadAsync<MusicBrainzDumpImportJobDocument>(documentId, cancellationToken);
        var document = MusicBrainzDumpImportJobDocument.FromDomain(job);

        if (existing is null)
        {
            await session.StoreAsync(document, documentId, cancellationToken);
        }
        else
        {
            var changeVector = session.Advanced.GetChangeVectorFor(existing);
            session.Advanced.Evict(existing);
            await session.StoreAsync(document, changeVector, documentId, cancellationToken);
        }

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException exception)
        {
            throw new InvalidOperationException(
                $"MusicBrainz dump job '{job.Id.Value}' was modified concurrently; retry the claim or progress save.",
                exception);
        }
    }
}
