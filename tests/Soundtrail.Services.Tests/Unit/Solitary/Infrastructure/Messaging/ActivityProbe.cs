using System.Diagnostics;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

/// <summary>
/// Isolates <c>Soundtrail.Messaging</c> activity stops to this test's ambient trace.
/// A root <see cref="Activity"/> becomes <see cref="Activity.Current"/> so SUT activities
/// parent under it; the process-global listener ignores stops from other tests' traces.
/// </summary>
internal sealed class ActivityProbe : IDisposable
{
    private readonly Activity root;
    private readonly ActivityTraceId traceId;
    private readonly ActivityListener listener;
    private readonly List<Activity> stopped = [];
    private readonly object sync = new();
    private bool disposed;

    private ActivityProbe(Activity root, ActivityListener listener)
    {
        this.root = root;
        traceId = root.TraceId;
        this.listener = listener;
    }

    public IReadOnlyList<Activity> Stopped
    {
        get
        {
            lock (sync)
            {
                return stopped.ToArray();
            }
        }
    }

    /// <summary>
    /// Prefers the last activity marked as error; otherwise the last stopped activity
    /// (outermost activities stop after their children).
    /// </summary>
    public Activity? LastStoppedActivity
    {
        get
        {
            lock (sync)
            {
                return stopped.LastOrDefault(static a => a.Status == ActivityStatusCode.Error)
                       ?? stopped.LastOrDefault();
            }
        }
    }

    public static ActivityProbe Start()
    {
        var root = new Activity("activity-probe").Start()
                   ?? throw new InvalidOperationException("Failed to start probe root activity.");

        ActivityProbe? probe = null;
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "Soundtrail.Messaging",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                // TraceId is stable under W3C ids (RootId/Id formats differ).
                if (probe is null || activity.TraceId != probe.traceId)
                {
                    return;
                }

                lock (probe.sync)
                {
                    probe.stopped.Add(activity);
                }
            }
        };

        ActivitySource.AddActivityListener(listener);
        probe = new ActivityProbe(root, listener);
        return probe;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        listener.Dispose();
        root.Dispose();
    }
}
