namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportShardState
{
    public MusicBrainzDumpImportShardState(
        MusicBrainzDumpImportPhase phase,
        int shardId,
        long lineOffset = 0,
        MusicBrainzDumpImportShardStatus status = MusicBrainzDumpImportShardStatus.Pending,
        MusicBrainzDumpImportLease? lease = null,
        string? lastError = null)
    {
        if (shardId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardId), shardId, "Shard id must be non-negative.");
        }

        if (lineOffset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineOffset), lineOffset, "Line offset must be non-negative.");
        }

        Phase = phase;
        ShardId = shardId;
        LineOffset = lineOffset;
        Status = status;
        Lease = lease;
        LastError = lastError;
    }

    public MusicBrainzDumpImportPhase Phase { get; }

    public int ShardId { get; }

    public long LineOffset { get; private set; }

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

    public void UpdateLineOffset(long lineOffset)
    {
        if (lineOffset < LineOffset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineOffset),
                lineOffset,
                $"Line offset cannot move backwards from {LineOffset}.");
        }

        LineOffset = lineOffset;
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
}
