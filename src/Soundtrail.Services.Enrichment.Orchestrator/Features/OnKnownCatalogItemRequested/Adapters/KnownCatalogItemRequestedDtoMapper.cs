using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnKnownCatalogItemRequested.Adapters;

internal static class KnownCatalogItemRequestedDtoMapper
{
    public static KnownCatalogItemRequested ToDomainObject(KnownCatalogItemRequestedDto dto) =>
        new(
            dto switch
            {
                { TrackId: not null } => KnownCatalogItem.ForTrack(TrackId.From(dto.TrackId)),
                { AlbumId: not null } => KnownCatalogItem.ForAlbum(AlbumId.From(dto.AlbumId)),
                { ArtistId: not null } => KnownCatalogItem.ForArtist(ArtistId.From(dto.ArtistId)),
                _ => throw new InvalidOperationException("Known catalog item request must contain an artist id, album id or track id.")
            },
            PlaybackProviderFilter.Parse(dto.Playback),
            dto.TrustLevel,
            dto.RiskScore,
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));
}
