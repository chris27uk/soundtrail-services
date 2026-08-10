using System.Collections.Concurrent;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

/// <summary>
/// Runs async work on a dedicated thread with a single-thread
/// <see cref="SynchronizationContext"/> so <c>await</c> continuations resume there
/// instead of competing for the saturated xUnit thread pool.
/// </summary>
internal static class DedicatedThreadTaskRunner
{
    public static Task RunAsync(Func<Task> work, string threadName) =>
        RunAsync(
            async () =>
            {
                await work().ConfigureAwait(false);
                return 0;
            },
            threadName);

    public static Task<T> RunAsync<T>(Func<Task<T>> work, string threadName)
    {
        ArgumentNullException.ThrowIfNull(work);
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var thread = new Thread(() =>
        {
            var context = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(context);
            try
            {
                var operation = work();
                operation.ContinueWith(
                    static (task, state) =>
                    {
                        var (completion, syncContext) =
                            ((TaskCompletionSource<T>, SingleThreadSynchronizationContext))state!;
                        try
                        {
                            if (task.IsFaulted)
                            {
                                completion.TrySetException(task.Exception!.InnerExceptions);
                            }
                            else if (task.IsCanceled)
                            {
                                completion.TrySetCanceled();
                            }
                            else
                            {
                                completion.TrySetResult(task.Result);
                            }
                        }
                        finally
                        {
                            syncContext.Complete();
                        }
                    },
                    (tcs, context),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                context.RunOnCurrentThread();
            }
            catch (Exception exception)
            {
                tcs.TrySetException(exception);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        })
        {
            IsBackground = true,
            Name = threadName
        };

        thread.Start();
        return tcs.Task;
    }

    /// <summary>
    /// Clears the captured sync context for the duration of <paramref name="work"/> so
    /// ASP.NET / Testcontainers <c>ConfigureAwait(false)</c> paths are not forced back
    /// onto the dedicated thread (avoids StartAsync deadlocks).
    /// </summary>
    public static async Task<T> WithThreadPoolContinuationsAsync<T>(Func<Task<T>> work)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            return await work().ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    public static async Task WithThreadPoolContinuationsAsync(Func<Task> work)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            await work().ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();

        public override void Post(SendOrPostCallback d, object? state) =>
            this.queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Current == this)
            {
                d(state);
                return;
            }

            using var done = new ManualResetEventSlim(false);
            Exception? exception = null;
            this.Post(
                s =>
                {
                    try
                    {
                        d(s);
                    }
                    catch (Exception caught)
                    {
                        exception = caught;
                    }
                    finally
                    {
                        done.Set();
                    }
                },
                state);
            done.Wait();
            if (exception is not null)
            {
                throw exception;
            }
        }

        public void Complete() => this.queue.CompleteAdding();

        public void RunOnCurrentThread()
        {
            foreach (var (callback, state) in this.queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }
}
