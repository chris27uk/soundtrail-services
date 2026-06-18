using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class SourceApiBudgetPortFake : IReserveSourceApiBudgetPort
{
    private readonly Dictionary<string, SourceApiBudgetReservationResult> resultsBySource = [];

    public List<SourceApiBudgetReservationRequest> Requests { get; } = [];

    public Task<SourceApiBudgetReservationResult> TryReserveAsync(
        SourceApiBudgetReservationRequest request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(resultsBySource.TryGetValue(request.Source.Value, out var result)
            ? result
            : SourceApiBudgetReservationResult.Reserved());
    }

    public void Reject(
        ProviderName source,
        DateTimeOffset retryAt,
        string reason) =>
        resultsBySource[source.Value] = SourceApiBudgetReservationResult.Deferred(retryAt, reason);
}
