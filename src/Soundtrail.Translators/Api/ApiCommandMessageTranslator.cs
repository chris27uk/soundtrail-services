using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Translators.Api;

public static class ApiCommandMessageTranslator
{
    public static CatalogSearchAttemptDto ToDto(SearchCatalogRequested request)
    {
        var query = request.SearchCriteria.Query ?? string.Empty;

        return new CatalogSearchAttemptDto(
            MusicSearchTermPersistentIdTranslator.ToPersistentId(request.SearchCriteria),
            query,
            request.Playback.ToString(),
            request.TrustLevel,
            request.RiskScore,
            request.OccurredAt,
            request.CorrelationId.Value);
    }

    public static SearchCatalogRequested ToDomainObject(CatalogSearchAttemptDto dto) =>
        new(
            !string.IsNullOrWhiteSpace(dto.Criteria)
                ? MusicSearchTermPersistentIdTranslator.ToDomainObject(dto.Criteria)
                : MusicSearchCriteria.ByQuery(dto.Query),
            PlaybackProviderFilter.Parse(dto.Playback),
            dto.TrustLevel,
            dto.RiskScore,
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));

    public static KnownArtistRequestedDto ToDto(KnownArtistRequested request) =>
        new(
            request.ArtistId.Value,
            request.OccurredAt,
            request.CorrelationId.Value);

    public static KnownArtistRequested ToDomainObject(KnownArtistRequestedDto dto) =>
        new(
            ArtistId.From(dto.ArtistId),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));

    public static KnownAlbumRequestedDto ToDto(KnownAlbumRequested request) =>
        new(
            request.AlbumId.Value,
            request.OccurredAt,
            request.CorrelationId.Value);

    public static KnownAlbumRequested ToDomainObject(KnownAlbumRequestedDto dto) =>
        new(
            AlbumId.From(dto.AlbumId),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));

    public static KnownTrackRequestedDto ToDto(KnownTrackRequested request) =>
        new(
            request.TrackId.Value,
            request.Playback.ToString(),
            request.OccurredAt,
            request.CorrelationId.Value);

    public static KnownTrackRequested ToDomainObject(KnownTrackRequestedDto dto) =>
        new(
            TrackId.From(dto.TrackId),
            PlaybackProviderFilter.Parse(dto.Playback),
            dto.OccurredAt,
            CorrelationId.From(dto.CorrelationId));
}
