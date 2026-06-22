using Microsoft.Extensions.Options;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets;

public sealed class SourceApiBudgetReservationService(
    ITryReserveSourceApiBudgetWindowPort windowReservationPort,
    IOptions<SourceApiBudgetsOptions> options) : IReserveSourceApiBudgetPort
{
    private readonly SourceApiBudgetsOptions options = options.Value;

    public async Task<SourceApiBudgetReservationResult> TryReserveAsync(
        SourceApiBudgetReservationRequest request,
        CancellationToken cancellationToken)
    {
        var policy = GetPolicy(request.Source);

        if (policy.MinimumSpacingSeconds is { } minimumSpacingSeconds)
        {
            var spacingResult = await TryReserveWindowAsync(
                request,
                maxRequests: 1,
                safetyMarginPercent: 0,
                windowSeconds: minimumSpacingSeconds,
                keyPrefix: "source-budget-spacing",
                cancellationToken);

            if (!spacingResult.Accepted)
            {
                return spacingResult;
            }
        }

        return await TryReserveWindowAsync(
            request,
            policy.MaxRequests,
            policy.SafetyMarginPercent,
            policy.WindowSeconds,
            "source-budget",
            cancellationToken);
    }

    private async Task<SourceApiBudgetReservationResult> TryReserveWindowAsync(
        SourceApiBudgetReservationRequest request,
        int maxRequests,
        int safetyMarginPercent,
        int windowSeconds,
        string keyPrefix,
        CancellationToken cancellationToken)
    {
        var windowStartedAt = AlignToWindow(request.RequestedAt, windowSeconds);
        var windowEndsAt = windowStartedAt.AddSeconds(windowSeconds);
        var reservation = await windowReservationPort.TryReserveAsync(
            new TryReserveSourceApiBudgetWindowRequest(
                request.Source,
                windowStartedAt,
                windowEndsAt,
                request.RequestedAmount,
                maxRequests,
                safetyMarginPercent,
                keyPrefix),
            cancellationToken);

        return reservation.Reserved
            ? SourceApiBudgetReservationResult.Reserved()
            : SourceApiBudgetReservationResult.Deferred(
                reservation.RetryAt,
                $"{request.Source.Value} budget temporarily unavailable");
    }

    private SourceApiBudgetPolicyOptions GetPolicy(ProviderName source) =>
        source == ProviderName.MusicBrainz
            ? options.MusicBrainz
            : source == ProviderName.Odesli
                ? options.Odesli
                : throw new ArgumentOutOfRangeException(nameof(source), source, "Unsupported source budget.");

    private static DateTimeOffset AlignToWindow(DateTimeOffset timestamp, int windowSeconds)
    {
        var utc = timestamp.ToUniversalTime();
        var ticks = utc.Ticks - (utc.Ticks % TimeSpan.FromSeconds(windowSeconds).Ticks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
