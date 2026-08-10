using System.Runtime.CompilerServices;
using Soundtrail.Services.Tests.EndToEnd;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests;

internal static class TestInfrastructureWarmup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // xUnit saturates the pool with parallel tests + sync-over-async fixtures.
        // E2E host warmup awaits (Redis/ASB/Raven/WebApplication) need spare workers
        // even when the waiter itself runs on a dedicated thread.
        EnsureThreadPoolHeadroom();

        try
        {
            // Overlap Redis/ASB discovery with the parallel suite (env / AppHost / Testcontainers).
            _ = LocalRedisTestServer.StartAsync();
            EndToEndHostFixture.EnsureWarmupStarted();
        }
        catch
        {
            // Surfaces when the E2E fixture InitializeAsync awaits the shared task.
        }
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
