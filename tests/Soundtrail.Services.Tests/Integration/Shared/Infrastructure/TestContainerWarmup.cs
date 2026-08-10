using Soundtrail.Services.Tests.EndToEnd.Shared;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Kicks off Service Bus emulator startup once Raven is up so container pull/start
/// overlaps the integration wave. Avoids Task.Run (thread-pool starvation delays it).
/// </summary>
internal static class TestContainerWarmup
{
    private static int serviceBusWarmupStarted;

    public static void EnsureServiceBusWarmupStarted()
    {
        if (Interlocked.Exchange(ref serviceBusWarmupStarted, 1) != 0)
        {
            return;
        }

        try
        {
            _ = LocalServiceBusEmulator.StartAsync();
        }
        catch
        {
            // Surfaces as SkipException on first StartAsync await.
        }
    }
}
