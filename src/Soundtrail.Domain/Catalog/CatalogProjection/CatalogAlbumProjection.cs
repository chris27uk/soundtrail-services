namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed record CatalogAlbumProjection(
    string AlbumId,
    string ArtistId,
    string Name,
    string NormalizedName,
    string ArtistName,
    string? MusicBrainzReleaseId,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    string? ArtworkUrl,
    DateOnly? ReleaseDate,
    DateTimeOffset UpdatedAt);
