using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Adapters;

public sealed class RavenCompareExchangeSourceApiBudgetPort(
    IDocumentStore documentStore,
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
                request.Source,
                request.RequestedAt,
                request.RequestedAmount,
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
            request.Source,
            request.RequestedAt,
            request.RequestedAmount,
            policy.MaxRequests,
            policy.SafetyMarginPercent,
            policy.WindowSeconds,
            keyPrefix: "source-budget",
            cancellationToken);
    }

    private async Task<SourceApiBudgetReservationResult> TryReserveWindowAsync(
        ProviderName source,
        DateTimeOffset requestedAt,
        int requestedAmount,
        int maxRequests,
        int safetyMarginPercent,
        int windowSeconds,
        string keyPrefix,
        CancellationToken cancellationToken)
    {
        var windowStartedAt = AlignToWindow(requestedAt, windowSeconds);
        var windowEndsAt = windowStartedAt.AddSeconds(windowSeconds);
        var key = $"{keyPrefix}/{source.Value.ToLowerInvariant()}/{windowStartedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var session = documentStore.OpenAsyncSession(new SessionOptions
            {
                TransactionMode = TransactionMode.ClusterWide
            });
            var current = await session.Advanced.ClusterTransaction
                .GetCompareExchangeValueAsync<SourceApiBudgetWindowRecordDto>(key, cancellationToken);

            var record = current?.Value ?? new SourceApiBudgetWindowRecordDto
            {
                Source = source.Value,
                WindowStartedAt = windowStartedAt,
                WindowEndsAt = windowEndsAt,
                MaxRequests = maxRequests,
                ReservedRequests = 0,
                SafetyMarginPercent = safetyMarginPercent
            };

            var safeMax = Math.Max(0, record.MaxRequests - (int)Math.Ceiling(record.MaxRequests * (record.SafetyMarginPercent / 100m)));
            if (record.ReservedRequests + requestedAmount > safeMax)
            {
                return SourceApiBudgetReservationResult.Deferred(
                    windowEndsAt,
                    $"{source.Value} budget temporarily unavailable");
            }

            var updated = new SourceApiBudgetWindowRecordDto
            {
                Source = record.Source,
                WindowStartedAt = record.WindowStartedAt,
                WindowEndsAt = record.WindowEndsAt,
                MaxRequests = record.MaxRequests,
                ReservedRequests = record.ReservedRequests + requestedAmount,
                SafetyMarginPercent = record.SafetyMarginPercent
            };

            try
            {
                if (current?.Value is null)
                {
                    session.Advanced.ClusterTransaction.CreateCompareExchangeValue(key, updated);
                }
                else
                {
                    current.Value.ReservedRequests = updated.ReservedRequests;
                }

                await session.SaveChangesAsync(cancellationToken);
                return SourceApiBudgetReservationResult.Reserved();
            }
            catch (ConcurrencyException)
            {
            }
        }

        return SourceApiBudgetReservationResult.Deferred(
            windowEndsAt,
            $"{source.Value} budget temporarily unavailable");
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

    public sealed class SourceApiBudgetWindowRecordDto
    {
        public string Source { get; set; } = string.Empty;

        public DateTimeOffset WindowStartedAt { get; set; }

        public DateTimeOffset WindowEndsAt { get; set; }

        public int MaxRequests { get; set; }

        public int ReservedRequests { get; set; }

        public int SafetyMarginPercent { get; set; }
    }
}
