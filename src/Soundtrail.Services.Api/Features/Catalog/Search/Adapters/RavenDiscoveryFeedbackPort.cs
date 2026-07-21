using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Api.Features.Catalog.Search.Contract;

namespace Soundtrail.Services.Api.Features.Catalog.Search.Adapters;

public sealed class RavenDiscoveryFeedbackPort(
    IDocumentStore documentStore) : IDiscoveryFeedbackPort
{
    public async Task<DiscoveryFeedbackResponse?> GetAsync(EnrichmentTarget target, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var record = await session.LoadAsync<CatalogDiscoveryFeedbackRecordDto>(
            CatalogDiscoveryFeedbackRecordDto.GetDocumentId(target.NormalisedIdentifier),
            cancellationToken);

        if (record is null)
        {
            return null;
        }

        return new DiscoveryFeedbackResponse(
            record.Status,
            Enum.TryParse<LookupPriorityBand>(record.Priority, true, out var priority) ? priority : LookupPriorityBand.Low,
            record.NextEligibleAtUtc,
            record.EarliestExpectedCompletionAtUtc,
            record.Reason,
            record.UpdatedAtUtc);
    }
}
