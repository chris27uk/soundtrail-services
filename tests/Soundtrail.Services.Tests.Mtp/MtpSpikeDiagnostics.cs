using System.Runtime.CompilerServices;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests.Mtp;

/// <summary>
/// Records wall-clock markers for comparing MTP scheduling vs the VSTest/xUnit 2 pack.
/// Output: <c>%TEMP%/soundtrail-mtp-spike.txt</c>
/// </summary>
internal static class MtpSpikeDiagnostics
{
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "soundtrail-mtp-spike.txt");

    public static long ModuleInitTick { get; private set; }

    public static long WarmupKickTick { get; private set; }

    public static long FirstTestStartTick { get; private set; }

    public static long EndToEndTestStartTick { get; private set; }

    [ModuleInitializer]
    internal static void ModuleInitialize()
    {
        ModuleInitTick = Environment.TickCount64;
        EnsureThreadPoolHeadroom();
        Log("moduleInit");

        try
        {
            WarmupKickTick = Environment.TickCount64;
            _ = LocalRedisTestServer.StartAsync();
            MtpEndToEndHostFixture.EnsureWarmupStarted();
            Log("warmupKicked");
        }
        catch (Exception exception)
        {
            Log($"warmupKickFailed {exception.GetType().Name}: {exception.Message}");
        }
    }

    public static void RecordTestStart(string testName)
    {
        var tick = Environment.TickCount64;
        if (FirstTestStartTick == 0)
        {
            FirstTestStartTick = tick;
            Log($"firstTestStart name={testName} sinceModuleInitMs={tick - ModuleInitTick}");
        }

        if (testName.Contains("EndToEnd", StringComparison.Ordinal)
            && EndToEndTestStartTick == 0)
        {
            EndToEndTestStartTick = tick;
            Log($"e2eTestStart name={testName} sinceModuleInitMs={tick - ModuleInitTick} sinceWarmupKickMs={tick - WarmupKickTick}");
        }
    }

    public static async Task RecordFixtureReadyAsync()
    {
        var tick = Environment.TickCount64;
        Log($"fixtureReady sinceModuleInitMs={tick - ModuleInitTick} sinceWarmupKickMs={tick - WarmupKickTick}");
        await Task.CompletedTask;
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

    private static void Log(string message)
    {
        try
        {
            File.AppendAllText(
                LogPath,
                $"{DateTime.UtcNow:o} {message} thread={Thread.CurrentThread.Name ?? "<unnamed>"}\n");
        }
        catch
        {
        }
    }
}
