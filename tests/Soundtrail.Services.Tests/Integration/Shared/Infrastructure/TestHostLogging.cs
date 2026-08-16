using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Test hosts share the process console with the test runner.
/// Clearing providers keeps MTP summary readable and avoids <c>fail:</c> lines
/// from negative tests (startup validation, unhealthy checks) when the run passes.
/// </summary>
internal static class TestHostLogging
{
    public static WebApplicationBuilder Quiet(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        return builder;
    }

    public static HostApplicationBuilder Quiet(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        return builder;
    }
}
