using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Model;

namespace Soundtrail.Domain.Catalog.IntegrationEvents;

public sealed record PlaybackReferencesResolutionRequiredIntegrationEvent(
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    CorrelationId CorrelationId,
    ProviderName SourceProvider,
    DateTimeOffset ObservedAt,
    MusicSearchTerm SearchTerm,
    string? ArtistId,
    string? AlbumId) : MusicTrackIntegrationEvent(MusicCatalogId);
