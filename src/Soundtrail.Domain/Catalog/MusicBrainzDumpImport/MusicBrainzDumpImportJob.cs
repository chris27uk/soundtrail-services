namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public sealed class MusicBrainzDumpImportJob
{
    private readonly Dictionary<string, MusicBrainzDumpImportShardState> shards = new(StringComparer.Ordinal);

    public MusicBrainzDumpImportJob(
        MusicBrainzDumpImportJobId id,
        string dumpVersion,
        DateTimeOffset requestedAt,
        MusicBrainzDumpImportJobStatus status = MusicBrainzDumpImportJobStatus.Pending,
        MusicBrainzDumpImportPhase currentPhase = MusicBrainzDumpImportPhase.Artists,
        MusicBrainzDumpImportLease? producerLease = null,
        double progressPercent = 0,
        string? lastError = null,
        bool cancellationRequested = false,
        DateTimeOffset? startedAt = null,
        DateTimeOffset? finishedAt = null,
        IEnumerable<MusicBrainzDumpImportShardState>? shardStates = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dumpVersion);

        Id = id;
        DumpVersion = dumpVersion.Trim();
        RequestedAt = requestedAt;
        Status = status;
        CurrentPhase = currentPhase;
        ProducerLease = producerLease;
        ProgressPercent = progressPercent;
        LastError = lastError;
        CancellationRequested = cancellationRequested;
        StartedAt = startedAt;
        FinishedAt = finishedAt;

        if (shardStates is null)
        {
            return;
        }

        foreach (var shard in shardStates)
        {
            shards[shard.Key] = shard;
        }
    }

    public MusicBrainzDumpImportJobId Id { get; }

    public string DumpVersion { get; }

    public DateTimeOffset RequestedAt { get; private set; }

    public MusicBrainzDumpImportJobStatus Status { get; private set; }

    public MusicBrainzDumpImportPhase CurrentPhase { get; private set; }

    public MusicBrainzDumpImportLease? ProducerLease { get; private set; }

    public double ProgressPercent { get; private set; }

    public string? LastError { get; private set; }

    public bool CancellationRequested { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public IReadOnlyCollection<MusicBrainzDumpImportShardState> Shards => shards.Values;

    public static MusicBrainzDumpImportJob CreateNew(
        MusicBrainzDumpImportJobId id,
        string dumpVersion,
        DateTimeOffset requestedAt) =>
        new(id, dumpVersion, requestedAt);

    public void PrepareForRetrigger(DateTimeOffset requestedAt)
    {
        if (Status is not (MusicBrainzDumpImportJobStatus.Completed
            or MusicBrainzDumpImportJobStatus.Failed
            or MusicBrainzDumpImportJobStatus.Cancelled))
        {
            RequestedAt = requestedAt;
            return;
        }

        Status = MusicBrainzDumpImportJobStatus.Pending;
        CurrentPhase = MusicBrainzDumpImportPhase.Artists;
        ProducerLease = null;
        ProgressPercent = 0;
        LastError = null;
        CancellationRequested = false;
        StartedAt = null;
        FinishedAt = null;
        RequestedAt = requestedAt;
        shards.Clear();
    }

    public bool TryClaimProducer(string owner, DateTimeOffset now, TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        if (CancellationRequested ||
            Status is MusicBrainzDumpImportJobStatus.Completed
                or MusicBrainzDumpImportJobStatus.Cancelled)
        {
            return false;
        }

        if (ProducerLease is { } lease && lease.IsActive(now) &&
            !string.Equals(lease.Owner, owner, StringComparison.Ordinal))
        {
            return false;
        }

        ProducerLease = new MusicBrainzDumpImportLease(owner, now.Add(leaseDuration));
        StartedAt ??= now;
        if (Status == MusicBrainzDumpImportJobStatus.Pending)
        {
            Status = MusicBrainzDumpImportJobStatus.Downloading;
        }

        return true;
    }

    public void HeartbeatProducer(string owner, DateTimeOffset now, TimeSpan leaseDuration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        if (ProducerLease is null ||
            !string.Equals(ProducerLease.Owner, owner, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Producer lease is not held by '{owner}'.");
        }

        ProducerLease = new MusicBrainzDumpImportLease(owner, now.Add(leaseDuration));
    }

    public void SetStatus(MusicBrainzDumpImportJobStatus status)
    {
        Status = status;
        if (status is MusicBrainzDumpImportJobStatus.Completed
            or MusicBrainzDumpImportJobStatus.Failed
            or MusicBrainzDumpImportJobStatus.Cancelled)
        {
            FinishedAt = DateTimeOffset.UtcNow;
            ProducerLease = null;
        }
    }

    public void SetProgressPercent(double progressPercent)
    {
        if (progressPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), progressPercent, "Progress must be between 0 and 100.");
        }

        ProgressPercent = progressPercent;
    }

    public void RequestCancellation() => CancellationRequested = true;

    public void SetLastError(string? error) => LastError = error;

    public MusicBrainzDumpImportShardState GetOrAddShard(MusicBrainzDumpImportPhase phase, int shardId)
    {
        var key = MusicBrainzDumpImportShardState.FormatKey(phase, shardId);
        if (shards.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = new MusicBrainzDumpImportShardState(phase, shardId);
        shards[key] = created;
        return created;
    }

    public bool TryClaimShard(
        MusicBrainzDumpImportPhase phase,
        int shardId,
        string owner,
        DateTimeOffset now,
        TimeSpan leaseDuration)
    {
        if (phase != CurrentPhase)
        {
            return false;
        }

        if (CancellationRequested)
        {
            return false;
        }

        var shard = GetOrAddShard(phase, shardId);
        if (!shard.TryClaim(owner, now, leaseDuration))
        {
            return false;
        }

        if (Status is MusicBrainzDumpImportJobStatus.Downloading
            or MusicBrainzDumpImportJobStatus.Extracting
            or MusicBrainzDumpImportJobStatus.Pending)
        {
            Status = MusicBrainzDumpImportJobStatus.Importing;
        }

        return true;
    }

    public void RegisterPhaseShards(MusicBrainzDumpImportPhase phase, int shardCount)
    {
        if (shardCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shardCount), shardCount, "Shard count must be positive.");
        }

        for (var shardId = 0; shardId < shardCount; shardId++)
        {
            GetOrAddShard(phase, shardId);
        }
    }

    public bool HasRegisteredShards(MusicBrainzDumpImportPhase phase) =>
        shards.Values.Any(shard => shard.Phase == phase);

    public bool TryCompleteRecordingsPhaseAsFinal(DateTimeOffset finishedAt)
    {
        if (CurrentPhase != MusicBrainzDumpImportPhase.Recordings ||
            !AreAllShardsCompleted(MusicBrainzDumpImportPhase.Recordings))
        {
            return false;
        }

        Status = MusicBrainzDumpImportJobStatus.Completed;
        FinishedAt = finishedAt;
        ProducerLease = null;
        ProgressPercent = 100;
        return true;
    }

    public bool AreAllShardsCompleted(MusicBrainzDumpImportPhase phase)
    {
        var phaseShards = shards.Values.Where(shard => shard.Phase == phase).ToArray();
        return phaseShards.Length > 0 &&
               phaseShards.All(static shard => shard.Status == MusicBrainzDumpImportShardStatus.Completed);
    }

    public bool TryAdvancePhase()
    {
        if (!AreAllShardsCompleted(CurrentPhase))
        {
            return false;
        }

        if (CurrentPhase == MusicBrainzDumpImportPhase.Recordings)
        {
            return false;
        }

        CurrentPhase = CurrentPhase switch
        {
            MusicBrainzDumpImportPhase.Artists => MusicBrainzDumpImportPhase.ReleaseGroups,
            MusicBrainzDumpImportPhase.ReleaseGroups => MusicBrainzDumpImportPhase.Recordings,
            _ => CurrentPhase
        };

        return true;
    }
}
