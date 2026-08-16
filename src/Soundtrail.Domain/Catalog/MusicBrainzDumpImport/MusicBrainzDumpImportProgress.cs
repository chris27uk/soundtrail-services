namespace Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

public static class MusicBrainzDumpImportProgress
{
    public const double PhaseSpan = 100.0 / 3.0;

    public static double PhaseBase(MusicBrainzDumpImportPhase phase) =>
        (int)phase * PhaseSpan;

    public static double AfterProducerPublished(MusicBrainzDumpImportPhase phase) =>
        Clamp(PhaseBase(phase) + (PhaseSpan / 2.0));

    public static double AfterShardCompleted(
        MusicBrainzDumpImportPhase phase,
        int completedShards,
        int totalShardsInPhase)
    {
        if (totalShardsInPhase <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalShardsInPhase), totalShardsInPhase, "Total shards must be positive.");
        }

        if (completedShards < 0 || completedShards > totalShardsInPhase)
        {
            throw new ArgumentOutOfRangeException(nameof(completedShards), completedShards, "Completed shards must be within the phase total.");
        }

        if (completedShards == totalShardsInPhase)
        {
            return Clamp(PhaseBase(phase) + PhaseSpan);
        }

        var midpoint = AfterProducerPublished(phase);
        return Clamp(midpoint + (PhaseSpan / 2.0) * (completedShards / (double)totalShardsInPhase));
    }

    public static double Terminal => 100.0;

    public static (int Completed, int Total) CountPhaseShards(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase)
    {
        ArgumentNullException.ThrowIfNull(job);

        var phaseShards = job.Shards.Where(shard => shard.Phase == phase).ToArray();
        var completed = phaseShards.Count(static shard =>
            shard.Status == MusicBrainzDumpImportShardStatus.Completed);
        return (completed, phaseShards.Length);
    }

    private static double Clamp(double progressPercent) =>
        Math.Clamp(progressPercent, 0.0, 100.0);
}
