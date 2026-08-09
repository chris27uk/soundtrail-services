using Raven.Client.Documents;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;

public sealed class RavenMusicBrainzDumpImportJobStore(IDocumentStore documentStore) : IMusicBrainzDumpImportJobStore
{
    public async Task<MusicBrainzDumpImportJob> EnsureAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);

        using var session = documentStore.OpenAsyncSession();
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
        await session.StoreAsync(
            MusicBrainzDumpImportJobDocument.FromDomain(job),
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
        var documentId = MusicBrainzDumpImportJobDocument.DocumentId(job.Id);
        await session.StoreAsync(
            MusicBrainzDumpImportJobDocument.FromDomain(job),
            documentId,
            cancellationToken);
        await session.SaveChangesAsync(cancellationToken);
    }
}
