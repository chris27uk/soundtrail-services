using System.Runtime.CompilerServices;
using Soundtrail.Services.Tests.EndToEnd;
using Soundtrail.Services.Tests.EndToEnd.Shared;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests;

internal static class TestInfrastructureWarmup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Start on the assembly-load thread — do not Task.Run. Under a saturated test
        // thread pool, queued work waits until the suite drains and E2E opens an idle gap.
        try
        {
            _ = LocalRedisTestServer.StartAsync();
            _ = LocalServiceBusEmulator.StartAsync();
            EndToEndHostFixture.EnsureWarmupStarted();
        }
        catch
        {
            // Surfaces when the E2E fixture InitializeAsync awaits the shared task.
        }
    }
}
