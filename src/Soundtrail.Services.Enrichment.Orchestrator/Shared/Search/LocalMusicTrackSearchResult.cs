using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed record LocalMusicTrackSearchResult(
    MusicCatalogId MusicCatalogId,
    string? Title,
    string? Artist,
    string? AlbumTitle,
    string? Isrc,
    string? Mbid,
    int? DurationMs,
    bool IsPlayable,
    ArtistId? ArtistId = null,
    AlbumId? AlbumId = null,
    DateOnly? ReleaseDate = null)
{
    public MusicSearchTerm? GetSearchTerm()
    {
        if (HasIsrc())
        {
            return MusicSearchTerm.ByIsrc(Isrc!);
        }

        if (HasEnoughMusicCharacteristicsForSearch())
        {
            return MusicSearchTerm.ByTrackArtistAlbum(Title!, Artist!, AlbumTitle);
        }
        
        return null;
    }

    private bool HasIsrc() => !string.IsNullOrWhiteSpace(Isrc);

    private bool HasEnoughMusicCharacteristicsForSearch() => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Artist);
}
