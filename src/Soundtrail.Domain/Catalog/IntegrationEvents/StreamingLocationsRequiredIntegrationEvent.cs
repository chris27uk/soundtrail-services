using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Catalog.IntegrationEvents;

public sealed record StreamingLocationsRequiredIntegrationEvent(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt,
    MusicSearchCriteria SearchCriteria,
    string? ArtistId,
    string? AlbumId) : MusicTrackIntegrationEvent(MusicCatalogId);
