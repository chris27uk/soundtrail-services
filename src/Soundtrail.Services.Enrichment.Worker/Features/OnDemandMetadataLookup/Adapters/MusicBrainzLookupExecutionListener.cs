using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;

public sealed class MusicBrainzLookupExecutionListener(
    OnDemandLookupMetadataHandler handler,
    ICatalogSearchTrackingStore catalogSearchTrackingStore,
    ICatalogSearchDiscoveryRepository discoveryRepository)
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
                    ToSearchTerm(dto),
                    ToHierarchy(dto)),
                cancellationToken);

            if (result.Started)
            {
                await AppendLifecycleEventsAsync(
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
                    result.Response.Hierarchy?.ArtistId?.Value,
                    result.Response.Hierarchy?.AlbumId?.Value,
                    result.Response.CorrelationId.Value)];
        }
        catch
        {
            await AppendLifecycleEventsAsync(
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.Priority,
                CatalogSearchLifecycleStatus.Failed,
                "Lookup failed",
                dto.CreatedAt,
                cancellationToken);
            throw;
        }
    }

    private async Task AppendLifecycleEventsAsync(
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
            var discovery = await CatalogSearchDiscovery.LoadAsync(discoveryRepository, tracking.Criteria, cancellationToken);

            switch (status)
            {
                case CatalogSearchLifecycleStatus.InProgress:
                    discovery.Start(priority, reason, updatedAt);
                    break;
                case CatalogSearchLifecycleStatus.Failed:
                    discovery.Fail(reason, updatedAt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            await discovery.SaveAsync(discoveryRepository, cancellationToken);
        }
    }

    private static MusicSearchTerm ToSearchTerm(LookupCanonicalMusicMetadataCommandDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Isrc)
            ? MusicSearchTerm.ByIsrc(dto.Isrc)
            : MusicSearchTerm.ByTrackArtistAlbum(
                dto.TrackName ?? string.Empty,
                dto.ArtistName ?? string.Empty,
                dto.AlbumName);

    private static CatalogTrackHierarchy? ToHierarchy(LookupCanonicalMusicMetadataCommandDto dto) =>
        dto.ArtistId is null && dto.AlbumId is null
            ? null
            : new CatalogTrackHierarchy(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId));
}
