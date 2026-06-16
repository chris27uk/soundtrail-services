using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.SourceApiBudgetReservation;

internal sealed class InProcessSourceApiBudgetPort(SourceApiBudgetsOptions options) : IReserveSourceApiBudgetPort
{
    private readonly Dictionary<string, int> reservationsByKey = [];

    public Task<SourceApiBudgetReservationResult> TryReserveAsync(
        SourceApiBudgetReservationRequest request,
        CancellationToken cancellationToken)
    {
        var policy = request.Source == ProviderName.MusicBrainz
            ? options.MusicBrainz
            : options.Odesli;

        if (policy.MinimumSpacingSeconds is { } minimumSpacingSeconds)
        {
            var spacingResult = TryReserveWindow(
                request.Source,
                request.RequestedAt,
                request.RequestedAmount,
                maxRequests: 1,
                safetyMarginPercent: 0,
                windowSeconds: minimumSpacingSeconds,
                "source-budget-spacing");
            if (!spacingResult.Accepted)
            {
                return Task.FromResult(spacingResult);
            }
        }

        return Task.FromResult(TryReserveWindow(
            request.Source,
            request.RequestedAt,
            request.RequestedAmount,
            policy.MaxRequests,
            policy.SafetyMarginPercent,
            policy.WindowSeconds,
            "source-budget"));
    }

    private SourceApiBudgetReservationResult TryReserveWindow(
        ProviderName source,
        DateTimeOffset requestedAt,
        int requestedAmount,
        int maxRequests,
        int safetyMarginPercent,
        int windowSeconds,
        string prefix)
    {
        var windowStartedAt = AlignToWindow(requestedAt, windowSeconds);
        var windowEndsAt = windowStartedAt.AddSeconds(windowSeconds);
        var key = $"{prefix}/{source.Value.ToLowerInvariant()}/{windowStartedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";
        var reservedRequests = reservationsByKey.TryGetValue(key, out var value) ? value : 0;
        var safeMax = Math.Max(0, maxRequests - (int)Math.Ceiling(maxRequests * (safetyMarginPercent / 100m)));

        if (reservedRequests + requestedAmount > safeMax)
        {
            return SourceApiBudgetReservationResult.Deferred(
                windowEndsAt,
                $"{source.Value} budget temporarily unavailable");
        }

        reservationsByKey[key] = reservedRequests + requestedAmount;
        return SourceApiBudgetReservationResult.Reserved();
    }

    private static DateTimeOffset AlignToWindow(DateTimeOffset timestamp, int windowSeconds)
    {
        var utc = timestamp.ToUniversalTime();
        var ticks = utc.Ticks - (utc.Ticks % TimeSpan.FromSeconds(windowSeconds).Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
