using System.Runtime.CompilerServices;
using Soundtrail.Services.Tests.EndToEnd;
using Soundtrail.Services.Tests.EndToEnd.Shared;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests;

internal static class TestInfrastructureWarmup
{
    private static readonly Lazy<Task> Bootstrap = new(StartBootstrapOnDedicatedThread);

    [ModuleInitializer]
    internal static void Initialize()
    {
        // xUnit saturates the pool with parallel tests + sync-over-async fixtures.
        // E2E host warmup awaits (Redis/ASB/Raven/WebApplication) need spare workers
        // even when the waiter itself runs on a dedicated thread.
        EnsureThreadPoolHeadroom();

        try
        {
            // Dedicated thread: container pull/start overlaps the full parallel suite.
            // Fire-and-forget on the thread pool loses to aggressive parallelization.
            _ = Bootstrap.Value;
            EndToEndHostFixture.EnsureWarmupStarted();
            // Prefer TestRunLifetime (assembly fixture) for ordered teardown before MTP's
            // foreground-thread wait. ProcessExit is a last-ditch fallback only.
            AppDomain.CurrentDomain.ProcessExit += static (_, _) =>
            {
                try
                {
                    EndToEndHostFixture.ShutdownSharedAsync().GetAwaiter().GetResult();
                    EmbeddedRavenTestServer.ShutdownAsync().AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // Best-effort; MTP may already have torn down the process.
                }
            };
        }
        catch
        {
            // Surfaces when a test first awaits shared infra.
        }
    }

    private static Task StartBootstrapOnDedicatedThread() =>
        DedicatedThreadTaskRunner.RunAsync(StartBootstrapAsync, "Soundtrail.TestInfra.Bootstrap");

    private static async Task StartBootstrapAsync()
    {
        await DedicatedThreadTaskRunner.WithThreadPoolContinuationsAsync(async () =>
        {
            var redisTask = LocalRedisTestServer.StartAsync();
            var serviceBusTask = LocalServiceBusEmulator.StartAsync();
            var ravenTask = Task.Run(EmbeddedRavenTestServer.EnsureServerStarted);
            await Task.WhenAll(redisTask, serviceBusTask, ravenTask);
        });
    }

    private static void EnsureThreadPoolHeadroom()
    {
        ThreadPool.GetMinThreads(out var worker, out var io);
        var targetWorker = Math.Max(worker, Environment.ProcessorCount + 32);
        var targetIo = Math.Max(io, Environment.ProcessorCount + 32);
        if (targetWorker != worker || targetIo != io)
        {
            ThreadPool.SetMinThreads(targetWorker, targetIo);
        }
    }
}
