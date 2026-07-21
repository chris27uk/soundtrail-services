namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged;

public sealed record CatalogSearchCandidateProjection(
    string CatalogItemId,
    string CandidateKind,
    string SearchText,
    string Title,
    string? ArtistName,
    string? AlbumTitle,
    string? ArtworkUrl,
    DateTimeOffset UpdatedAt);
