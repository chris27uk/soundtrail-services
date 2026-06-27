namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

public sealed record CatalogArtistProjection(
    string ArtistId,
    string Name,
    string NormalizedName,
    string? MusicBrainzArtistId,
    IReadOnlyList<string> AvailableProviders,
    IReadOnlyList<string> TerminallyUnavailableProviders,
    string? ArtworkUrl,
    DateTimeOffset UpdatedAt);
