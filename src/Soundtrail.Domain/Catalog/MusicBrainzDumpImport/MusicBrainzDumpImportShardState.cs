namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportShardState
{
    public MusicBrainzDumpImportShardState(
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long lineOffset = 0,
        long projectionLineOffset = 0,
        MusicBrainzDumpImportShardStatus status = MusicBrainzDumpImportShardStatus.Pending,
        MusicBrainzDumpImportLease? lease = null,
        string? lastError = null,
        long lineByteOffset = 0,
        long projectionByteOffset = 0)
    {
        if (shardId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardId), shardId, "Shard id must be non-negative.");
        }

        if (lineOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineOffset), lineOffset, "Line offset must be non-negative.");
        }

        if (projectionLineOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectionLineOffset),
                projectionLineOffset,
                "Projection line offset must be non-negative.");
        }

        if (lineByteOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineByteOffset), lineByteOffset, "Line byte offset must be non-negative.");
        }

        if (projectionByteOffset < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectionByteOffset),
                projectionByteOffset,
                "Projection byte offset must be non-negative.");
        }

        Phase = phase;
        ShardId = shardId;
        LineOffset = lineOffset;
        ProjectionLineOffset = projectionLineOffset;
        LineByteOffset = lineByteOffset;
        ProjectionByteOffset = projectionByteOffset;
        Status = status;
        Lease = lease;
        LastError = lastError;
    }

    public MusicBrainzDumpImportPhase Phase { get; }

    public int ShardId { get; }

    public long LineOffset { get; private set; }

    /// <summary>
    /// Shard JSONL lines whose catalog projection has been flushed.
    /// Independent of <see cref="LineOffset"/> (event append). Resume walks the same file from this cursor.
    /// </summary>
    public long ProjectionLineOffset { get; private set; }

    /// <summary>
    /// File byte position after <see cref="LineOffset"/> lines (for seek-based append resume).
    /// </summary>
    public long LineByteOffset { get; private set; }

    /// <summary>
    /// File byte position after <see cref="ProjectionLineOffset"/> lines (for seek-based projection resume).
    /// </summary>
    public long ProjectionByteOffset { get; private set; }

    public MusicBrainzDumpImportShardStatus Status { get; private set; }

    public MusicBrainzDumpImportLease? Lease { get; private set; }

    public string? LastError { get; private set; }

    public string Key => FormatKey(Phase, ShardId);

    public static string FormatKey(MusicBrainzDumpImportPhase phase, int shardId) =>
        $"{phase}:{shardId}";

    public bool TryClaim(string owner, DateTimeOffset now, TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        if (Status == MusicBrainzDumpImportShardStatus.Completed)
        {
            return false;
        }

        if (Lease is { } lease && lease.IsActive(now) &&
            !string.Equals(lease.Owner, owner, StringComparison.Ordinal))
        {
            return false;
        }

        Status = MusicBrainzDumpImportShardStatus.Leased;
        Lease = new MusicBrainzDumpImportLease(owner, now.Add(leaseDuration));
        LastError = null;
        return true;
    }

    public void Heartbeat(string owner, DateTimeOffset now, TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        if (Status != MusicBrainzDumpImportShardStatus.Leased ||
            Lease is null ||
            !string.Equals(Lease.Owner, owner, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Shard '{Key}' is not leased by '{owner}'.");
        }

        Lease = new MusicBrainzDumpImportLease(owner, now.Add(leaseDuration));
    }

    public void UpdateLineOffset(long lineOffset, long? lineByteOffset = null)
    {
        if (lineOffset < LineOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineOffset),
                lineOffset,
                $"Line offset cannot move backwards from {LineOffset}.");
        }

        LineOffset = lineOffset;
        if (lineByteOffset is { } bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            LineByteOffset = bytes;
        }
    }

    public void UpdateProjectionLineOffset(long projectionLineOffset, long? projectionByteOffset = null)
    {
        if (projectionLineOffset < ProjectionLineOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectionLineOffset),
                projectionLineOffset,
                $"Projection line offset cannot move backwards from {ProjectionLineOffset}.");
        }

        ProjectionLineOffset = projectionLineOffset;
        if (projectionByteOffset is { } bytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bytes);
            ProjectionByteOffset = bytes;
        }
    }

    public void MarkCompleted()
    {
        Status = MusicBrainzDumpImportShardStatus.Completed;
        Lease = null;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Status = MusicBrainzDumpImportShardStatus.Failed;
        Lease = null;
        LastError = error;
    }

    /// <summary>
    /// Clears projection progress so deferred browse/search materialization can run again
    /// without re-appending events (<see cref="LineOffset"/> is preserved).
    /// </summary>
    public void ResetProjectionForRerun()
    {
        ProjectionLineOffset = 0;
        ProjectionByteOffset = 0;
        Status = MusicBrainzDumpImportShardStatus.Pending;
        Lease = null;
        LastError = null;
    }
}
