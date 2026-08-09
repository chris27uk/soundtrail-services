using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

internal sealed class MusicBrainzDumpImportJobStoreFake : IMusicBrainzDumpImportJobStore
{
    private readonly Dictionary<string, MusicBrainzDumpImportJob> jobs = new(StringComparer.Ordinal);

    public IReadOnlyCollection<MusicBrainzDumpImportJob> Jobs => jobs.Values;

    public Task<MusicBrainzDumpImportJob> EnsureAsync(
        MusicBrainzDumpImportJobId jobId,
        string dumpVersion,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken = default)
    {
        if (jobs.TryGetValue(jobId.Value, out var existing))
        {
            existing.PrepareForRetrigger(requestedAt);
            return Task.FromResult(existing);
        }

        var created = MusicBrainzDumpImportJob.CreateNew(jobId, dumpVersion, requestedAt);
        jobs[jobId.Value] = created;
        return Task.FromResult(created);
    }

    public Task<MusicBrainzDumpImportJob?> GetAsync(
        MusicBrainzDumpImportJobId jobId,
        CancellationToken cancellationToken = default)
    {
        jobs.TryGetValue(jobId.Value, out var job);
        return Task.FromResult(job);
    }

    public Task SaveAsync(
        MusicBrainzDumpImportJob job,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        jobs[job.Id.Value] = job;
        return Task.CompletedTask;
    }
}
