using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.CatalogBrowsing;

public sealed record TrackSummary(
    TrackId TrackId,
    string Title,
    AlbumId AlbumId,
    string AlbumName,
    string? Isrc,
    int? DurationMs,
    PlayabilityStatus PlayabilityStatus,
    IReadOnlyList<ProviderName> AvailableProviders,
    IReadOnlyList<ProviderName> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReference> ProviderReferences);
