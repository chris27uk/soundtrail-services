using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.SourceApiBudgetReservation;

internal sealed class InProcessSourceApiBudgetPort : ITryReserveSourceApiBudgetWindowPort
{
    private readonly Dictionary<string, int> reservationsByKey = [];

    public Task<TryReserveSourceApiBudgetWindowResult> TryReserveAsync(
        TryReserveSourceApiBudgetWindowRequest request,
        CancellationToken cancellationToken)
    {
        var key = $"{request.KeyPrefix}/{request.Source.Value.ToLowerInvariant()}/{request.WindowStartedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";
        var reservedRequests = reservationsByKey.TryGetValue(key, out var value) ? value : 0;
        var safeMax = Math.Max(0, request.MaxRequests - (int)Math.Ceiling(request.MaxRequests * (request.SafetyMarginPercent / 100m)));

        if (reservedRequests + request.RequestedAmount > safeMax)
        {
            return Task.FromResult(TryReserveSourceApiBudgetWindowResult.Rejected(request.WindowEndsAt));
        }

        reservationsByKey[key] = reservedRequests + request.RequestedAmount;
        return Task.FromResult(TryReserveSourceApiBudgetWindowResult.Success(request.WindowEndsAt));
    }
}
