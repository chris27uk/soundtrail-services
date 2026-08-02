using Microsoft.Net.Http.Headers;

namespace Soundtrail.Services.Api.Features.Catalog.Shared.Adapters;

internal static class DiscoveryResponseHeaders
{
    public static void Apply(HttpContext httpContext, DiscoveryFeedbackResponseDto? discovery)
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        if (discovery?.NextEligibleAtUtc is not { } nextEligibleAtUtc)
        {
            return;
        }

        var retryAfterSeconds = (int)Math.Ceiling((nextEligibleAtUtc - DateTimeOffset.UtcNow).TotalSeconds);
        if (retryAfterSeconds > 0)
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfterSeconds.ToString();
        }
    }
}
