using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;

public sealed class MusicBrainzLookupExecutionListener(
    OnDemandLookupMetadataHandler handler,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    IUpsertCatalogSearchStatusPort upsertCatalogSearchStatusPort)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        LookupCanonicalMusicMetadataCommandDto dto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await handler.Handle(
                new LookupMusicMetadataCommand(
                    CommandId.From(dto.CommandId),
                    MusicCatalogId.From(dto.MusicCatalogId),
                    dto.Priority,
                    dto.CreatedAt,
                    CorrelationId.From(dto.CorrelationId),
                    ToSearchTerm(dto)),
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
                    result.Response.Metadata is null
                        ? null
                        : new SongMetadataDto(
                            result.Response.Metadata.Title,
                            result.Response.Metadata.Artist,
                            result.Response.Metadata.Isrc,
                            result.Response.Metadata.Mbid,
                            result.Response.Metadata.DurationMs),
                    result.Response.References.Select(reference => new ExternalReferenceDto(
                        reference.Provider.Value,
                        reference.Url,
                        reference.ExternalId)).ToArray(),
                    result.Response.FailedProviders.Select(failure => new ProviderLookupFailureDto(
                        failure.Provider.Value,
                        failure.SourceProvider.Value)).ToArray(),
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

    private static MusicSearchTerm ToSearchTerm(LookupCanonicalMusicMetadataCommandDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Isrc)
            ? MusicSearchTerm.ByIsrc(dto.Isrc)
            : MusicSearchTerm.ByTrackArtistAlbum(
                dto.TrackName ?? string.Empty,
                dto.ArtistName ?? string.Empty,
                dto.AlbumName);
}
