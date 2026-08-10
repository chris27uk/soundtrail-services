using System.Runtime.CompilerServices;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

namespace Soundtrail.Services.Tests;

internal static class TestInfrastructureWarmup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Start Redis immediately on the assembly-load thread (not Task.Run) so it overlaps
        // the suite. Service Bus stays lazy: starting it here raced host wiring in E2E.
        try
        {
            _ = LocalRedisTestServer.StartAsync();
        }
        catch
        {
            // Surfaces on first StartAsync await.
        }
    }
}
