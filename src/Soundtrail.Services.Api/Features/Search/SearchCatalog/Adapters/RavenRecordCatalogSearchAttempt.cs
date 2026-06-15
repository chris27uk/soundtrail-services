using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using System.Text.Json;

namespace Soundtrail.Services.Api.Features.Search.SearchCatalog.Adapters;

public sealed class RavenRecordCatalogSearchAttempt(
    IDocumentStore documentStore,
    IQueueCatalogSearchAttemptPort queueCatalogSearchAttemptPort) : IRecordCatalogSearchAttemptPort
{
    private const string DiscoveryRequestedEventType = "DiscoveryRequested";

    public async Task<bool> TryRequestAsync(
        RecordCatalogSearchAttemptCommand command,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        session.Advanced.UseOptimisticConcurrency = true;

        var streamId = DiscoveryQueryEventStreamMetadataRecordDto.GetDocumentId(command.Criteria.Value);
        var existing = await session.LoadAsync<DiscoveryQueryEventStreamMetadataRecordDto>(streamId, cancellationToken);
        if (existing is not null)
        {
            return false;
        }

        var metadata = new DiscoveryQueryEventStreamMetadataRecordDto
        {
            Id = streamId,
            Criteria = command.Criteria.Value,
            Version = 1,
            UpdatedAtUtc = command.Request.OccurredAt
        };

        var storedEvent = new DiscoveryQueryStoredEventRecordDto
        {
            Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(command.Criteria.Value, 1),
            Criteria = command.Criteria.Value,
            Version = 1,
            EventType = DiscoveryRequestedEventType,
            Data = JsonSerializer.Serialize(new DiscoveryRequestedEventDataRecordDto(
                command.Criteria.Value,
                command.Request.Query.Value,
                command.Request.TrustLevel,
                command.Request.RiskScore,
                command.Request.OccurredAt,
                command.Request.CorrelationId.Value)),
            OccurredAtUtc = command.Request.OccurredAt,
            CorrelationId = command.Request.CorrelationId.Value
        };

        await session.StoreAsync(metadata, cancellationToken);
        await session.StoreAsync(storedEvent, cancellationToken);

        try
        {
            await session.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyException)
        {
            return false;
        }

        await queueCatalogSearchAttemptPort.EnqueueAsync(command.Request, cancellationToken);
        return true;
    }
}
