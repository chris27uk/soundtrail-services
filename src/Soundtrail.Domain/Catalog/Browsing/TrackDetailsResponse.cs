using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog.Browsing;

public sealed record TrackDetailsResponse(
    ArtistId ArtistId,
    string ArtistName,
    AlbumId AlbumId,
    string AlbumName,
    TrackId TrackId,
    string Title,
    string? Isrc,
    int? DurationMs,
    PlayabilityStatus PlayabilityStatus,
    IReadOnlyList<ProviderName> AvailableProviders,
    IReadOnlyList<ProviderName> TerminallyUnavailableProviders,
    IReadOnlyList<ProviderReference> ProviderReferences);
