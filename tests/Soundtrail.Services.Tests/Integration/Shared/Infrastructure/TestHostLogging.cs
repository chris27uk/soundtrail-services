using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Test <see cref="WebApplication"/> hosts share the process console with the test runner.
/// Clearing providers keeps MTP progress/summary readable in local and CI logs.
/// </summary>
internal static class TestHostLogging
{
    public static WebApplicationBuilder Quiet(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        return builder;
    }
}
