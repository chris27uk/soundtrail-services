using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;

public sealed class PlaybackReferencesLookupExecutionListener(
    ExecutePlaybackReferencesLookupHandler handler,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IUpsertCatalogSearchStatusPort upsertCatalogSearchStatusPort)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        ResolvePlaybackReferencesCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await handler.Handle(
                new ResolvePlaybackReferencesCommand(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    dto.CreatedAt,
                    CorrelationId.From(dto.CorrelationId),
                    dto.SearchTerm.Isrc == null ? MusicSearchTerm.ByTrackArtistAlbum(dto.SearchTerm.Title!, dto.SearchTerm.Artist!,dto.SearchTerm.Album) : MusicSearchTerm.ByIsrc(dto.SearchTerm.Isrc)),
                cancellationToken);

            if (result.Started)
            {
                await ProjectStatusAsync(
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    CatalogSearchLifecycleStatus.InProgress,
                    "Lookup started",
                    dto.CreatedAt,
                    cancellationToken);
            }

            return result.Response is null
                ? []
                : [new EnrichmentResponseDto(
                    result.Response.CommandId.Value,
                    result.Response.MusicCatalogId.Value,
                    result.Response.SourceProvider.Value,
                    result.Response.Priority,
                    result.Response.CreatedAt,
                    null,
                    result.Response.References.Select(reference => new ExternalReferenceDto(
                        reference.Provider.Value,
                        reference.Url,
                        reference.ExternalId)).ToArray(),
                    result.Response.CorrelationId.Value)];
        }
        catch
        {
            await ProjectStatusAsync(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                CatalogSearchLifecycleStatus.Failed,
                "Lookup failed",
                dto.CreatedAt,
                cancellationToken);
            throw;
        }
    }

    private async Task ProjectStatusAsync(
        MusicCatalogId musicCatalogId,
        LookupPriorityBand priority,
        CatalogSearchLifecycleStatus status,
        string reason,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var trackings = await catalogSearchTrackingStore.GetByMusicCatalogIdAsync(musicCatalogId, cancellationToken);
        foreach (var tracking in trackings)
        {
            await upsertCatalogSearchStatusPort.UpsertAsync(
                new CatalogSearchStatusUpdate(
                    tracking.Criteria,
                    status,
                    priority,
                    WillBeLookedUp: status == CatalogSearchLifecycleStatus.InProgress,
                    EstimatedRetryAfterSeconds: null,
                    EarliestExpectedCompletionAt: null,
                    Reason: reason,
                    UpdatedAt: updatedAt),
                cancellationToken);
        }
    }
}
