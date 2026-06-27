namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed record CatalogTrackProjection(
    string TrackId,
    string ArtistId,
    string AlbumId,
    string Title,
    string NormalizedTitle,
    string ArtistName,
    string AlbumName,
    string SearchText,
    string? MusicBrainzRecordingId,
    string? Isrc,
    int? DurationMs,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    IReadOnlyList<CatalogProviderReferenceProjection> ProviderReferences,
    string? ArtworkUrl,
    DateTimeOffset UpdatedAt);
