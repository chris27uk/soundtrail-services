using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Translators.Discovery;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnAssessMusicTrack.Adapters;

public sealed class AssessMusicTrackListener(AssessMusicTrackHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public Task Handle(
        AssessMusicTrackCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var command = new AssessMusicTrackCommand(
            CommandId.For(dto.CommandId),
            CorrelationId.From(dto.CorrelationId),
            dto.CreatedAt,
            dto.Priority,
            MusicCatalogId.From(dto.MusicCatalogId),
            string.IsNullOrWhiteSpace(dto.Criteria) ? null : MusicSearchTermPersistentIdTranslator.ToDomainObject(dto.Criteria),
            dto.TrustLevel,
            dto.RiskScore);

        return handler.Handle(command, cancellationToken);
    }
}
