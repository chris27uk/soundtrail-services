using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Telemetry;

public static class MusicBrainzDumpImportTelemetry
{
    // Matches Host ApplicationName / ServiceDefaults AddSource for this executable.
    private static readonly string ApplicationSourceName =
        typeof(MusicBrainzDumpImportTelemetry).Assembly.GetName().Name
        ?? "Soundtrail.Services.Enrichment.CatalogImport";

    private static readonly ActivitySource ActivitySource = new(ApplicationSourceName);
    private static readonly Meter Meter = new(ApplicationSourceName);

    private static readonly ConcurrentDictionary<string, double> ProgressByJobId = new(StringComparer.Ordinal);
    private static readonly Counter<long> RowsImported = Meter.CreateCounter<long>(
        "soundtrail.musicbrainz_dump.rows_imported");
    private static readonly Counter<long> RowsSkipped = Meter.CreateCounter<long>(
        "soundtrail.musicbrainz_dump.rows_skipped");
    private static readonly Counter<long> JobTerminal = Meter.CreateCounter<long>(
        "soundtrail.musicbrainz_dump.job_terminal");

    static MusicBrainzDumpImportTelemetry()
    {
        Meter.CreateObservableGauge(
            "soundtrail.musicbrainz_dump.job.progress_percent",
            static () => ProgressByJobId.Select(static pair =>
                new Measurement<double>(
                    pair.Value,
                    new KeyValuePair<string, object?>("job_id", pair.Key))));
    }

    public static Activity? StartProducerPhaseActivity(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase)
    {
        ArgumentNullException.ThrowIfNull(job);

        var activity = ActivitySource.StartActivity(
            "musicbrainz.dump.producer.phase",
            ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("soundtrail.job_id", job.Id.Value);
        activity.SetTag("soundtrail.dump_version", job.DumpVersion);
        activity.SetTag("soundtrail.phase", phase.ToString());
        activity.SetTag("soundtrail.job_status", job.Status.ToString());
        return activity;
    }

    public static Activity? StartShardImportActivity(
        MusicBrainzDumpImportJob job,
        MusicBrainzDumpImportPhase phase,
        int shardId)
    {
        ArgumentNullException.ThrowIfNull(job);

        var activity = ActivitySource.StartActivity(
            "musicbrainz.dump.consumer.shard",
            ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("soundtrail.job_id", job.Id.Value);
        activity.SetTag("soundtrail.dump_version", job.DumpVersion);
        activity.SetTag("soundtrail.phase", phase.ToString());
        activity.SetTag("soundtrail.shard_id", shardId);
        activity.SetTag("soundtrail.job_status", job.Status.ToString());
        return activity;
    }

    public static void RecordProgress(MusicBrainzDumpImportJob job, double progressPercent)
    {
        ArgumentNullException.ThrowIfNull(job);
        job.SetProgressPercent(progressPercent);
        ProgressByJobId[job.Id.Value] = progressPercent;
        Activity.Current?.SetTag("soundtrail.progress_percent", progressPercent);
    }

    public static void RecordRows(string jobId, long imported, long skipped)
    {
        if (imported > 0)
        {
            RowsImported.Add(imported, new KeyValuePair<string, object?>("job_id", jobId));
        }

        if (skipped > 0)
        {
            RowsSkipped.Add(skipped, new KeyValuePair<string, object?>("job_id", jobId));
        }
    }

    public static void MarkJobTerminal(MusicBrainzDumpImportJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        RecordProgress(job, MusicBrainzDumpImportProgress.Terminal);
        JobTerminal.Add(
            1,
            new KeyValuePair<string, object?>("job_id", job.Id.Value),
            new KeyValuePair<string, object?>("status", job.Status.ToString()));

        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        activity.SetTag("soundtrail.job_status", job.Status.ToString());
        activity.SetStatus(
            job.Status == MusicBrainzDumpImportJobStatus.Completed
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error,
            job.LastError);
        activity.AddEvent(new ActivityEvent(
            "musicbrainz.dump.job.terminal",
            tags: new ActivityTagsCollection
            {
                { "soundtrail.job_id", job.Id.Value },
                { "soundtrail.job_status", job.Status.ToString() }
            }));
    }
}
