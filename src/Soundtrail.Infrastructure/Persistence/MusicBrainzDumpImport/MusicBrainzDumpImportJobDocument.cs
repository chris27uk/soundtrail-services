using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;

internal sealed class MusicBrainzDumpImportJobDocument
{
    public required string Id { get; init; }

    public required string DumpVersion { get; init; }

    public required DateTimeOffset RequestedAt { get; init; }

    public required string Status { get; init; }

    public required string CurrentPhase { get; init; }

    public string? ProducerLeaseOwner { get; init; }

    public DateTimeOffset? ProducerLeaseExpiresAt { get; init; }

    public double ProgressPercent { get; init; }

    public string? LastError { get; init; }

    public bool CancellationRequested { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public List<MusicBrainzDumpImportShardDocument> Shards { get; init; } = [];

    public static string DocumentId(MusicBrainzDumpImportJobId jobId) => jobId.Value;

    public static MusicBrainzDumpImportJobDocument FromDomain(MusicBrainzDumpImportJob job) =>
        new()
        {
            Id = DocumentId(job.Id),
            DumpVersion = job.DumpVersion,
            RequestedAt = job.RequestedAt,
            Status = job.Status.ToString(),
            CurrentPhase = job.CurrentPhase.ToString(),
            ProducerLeaseOwner = job.ProducerLease?.Owner,
            ProducerLeaseExpiresAt = job.ProducerLease?.ExpiresAt,
            ProgressPercent = job.ProgressPercent,
            LastError = job.LastError,
            CancellationRequested = job.CancellationRequested,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            Shards = job.Shards.Select(MusicBrainzDumpImportShardDocument.FromDomain).ToList()
        };

    public MusicBrainzDumpImportJob ToDomain() =>
        new(
            MusicBrainzDumpImportJobId.From(Id),
            DumpVersion,
            RequestedAt,
            Enum.Parse<MusicBrainzDumpImportJobStatus>(Status),
            Enum.Parse<MusicBrainzDumpImportPhase>(CurrentPhase),
            ProducerLeaseOwner is null || ProducerLeaseExpiresAt is null
                ? null
                : new MusicBrainzDumpImportLease(ProducerLeaseOwner, ProducerLeaseExpiresAt.Value),
            ProgressPercent,
            LastError,
            CancellationRequested,
            StartedAt,
            FinishedAt,
            Shards.Select(static shard => shard.ToDomain()));
}

internal sealed class MusicBrainzDumpImportShardDocument
{
    public required string Phase { get; init; }

    public required int ShardId { get; init; }

    public long LineOffset { get; init; }

    public required string Status { get; init; }

    public string? LeaseOwner { get; init; }

    public DateTimeOffset? LeaseExpiresAt { get; init; }

    public string? LastError { get; init; }

    public static MusicBrainzDumpImportShardDocument FromDomain(MusicBrainzDumpImportShardState shard) =>
        new()
        {
            Phase = shard.Phase.ToString(),
            ShardId = shard.ShardId,
            LineOffset = shard.LineOffset,
            Status = shard.Status.ToString(),
            LeaseOwner = shard.Lease?.Owner,
            LeaseExpiresAt = shard.Lease?.ExpiresAt,
            LastError = shard.LastError
        };

    public MusicBrainzDumpImportShardState ToDomain() =>
        new(
            Enum.Parse<MusicBrainzDumpImportPhase>(Phase),
            ShardId,
            LineOffset,
            Enum.Parse<MusicBrainzDumpImportShardStatus>(Status),
            LeaseOwner is null || LeaseExpiresAt is null
                ? null
                : new MusicBrainzDumpImportLease(LeaseOwner, LeaseExpiresAt.Value),
            LastError);
}
