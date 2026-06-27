using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

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
    IReadOnlyList<ProviderName>? AvailableProviders = null,
    ArtistId? ArtistId = null,
    AlbumId? AlbumId = null,
    DateOnly? ReleaseDate = null)
{
    public IReadOnlyList<ProviderName> AvailableProviders { get; init; } = AvailableProviders ?? [];

    public bool CanCreateSearchTerm() => HasIsrc() || HasEnoughMusicCharacteristicsForSearch();

    public bool RequiresStreamingLocations(PlaybackProviderFilter playback) =>
        playback.RequiresAnyMissing(AvailableProviders);

    public MusicSearchCriteria ToSearchTerm()
    {
        if (HasIsrc())
        {
            return MusicSearchCriteria.ByIsrc(Isrc!);
        }

        if (HasEnoughMusicCharacteristicsForSearch())
        {
            return MusicSearchCriteria.ByTrackArtistAlbum(Title!, Artist!, AlbumTitle);
        }

        throw new InvalidOperationException("Cannot create a music search term for a track without an ISRC or track and artist details.");
    }

    private bool HasIsrc() => !string.IsNullOrWhiteSpace(Isrc);

    private bool HasEnoughMusicCharacteristicsForSearch() => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Artist);
}
