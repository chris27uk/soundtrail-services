using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Adapters;

public sealed class RavenCompareExchangeSourceApiBudgetPort(
    IDocumentStore documentStore) : ITryReserveSourceApiBudgetWindowPort
{
    public async Task<TryReserveSourceApiBudgetWindowResult> TryReserveAsync(
        TryReserveSourceApiBudgetWindowRequest request,
        CancellationToken cancellationToken)
    {
        var key = $"{request.KeyPrefix}/{request.Source.Value.ToLowerInvariant()}/{request.WindowStartedAt.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";

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
                Source = request.Source.Value,
                WindowStartedAt = request.WindowStartedAt,
                WindowEndsAt = request.WindowEndsAt,
                MaxRequests = request.MaxRequests,
                ReservedRequests = 0,
                SafetyMarginPercent = request.SafetyMarginPercent
            };

            var safeMax = Math.Max(0, record.MaxRequests - (int)Math.Ceiling(record.MaxRequests * (record.SafetyMarginPercent / 100m)));
            if (record.ReservedRequests + request.RequestedAmount > safeMax)
            {
                return TryReserveSourceApiBudgetWindowResult.Rejected(request.WindowEndsAt);
            }

            var updated = new SourceApiBudgetWindowRecordDto
            {
                Source = record.Source,
                WindowStartedAt = record.WindowStartedAt,
                WindowEndsAt = record.WindowEndsAt,
                MaxRequests = record.MaxRequests,
                ReservedRequests = record.ReservedRequests + request.RequestedAmount,
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
                return TryReserveSourceApiBudgetWindowResult.Success(request.WindowEndsAt);
            }
            catch (ConcurrencyException)
            {
            }
        }

        return TryReserveSourceApiBudgetWindowResult.Rejected(request.WindowEndsAt);
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
