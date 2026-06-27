using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed record LocalMusicTrackSearchResult
{
    public LocalMusicTrackSearchResult(
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
        this.MusicCatalogId = MusicCatalogId;
        this.Title = Title;
        this.Artist = Artist;
        this.AlbumTitle = AlbumTitle;
        this.Isrc = Isrc;
        this.Mbid = Mbid;
        this.DurationMs = DurationMs;
        this.IsPlayable = IsPlayable;
        this.AvailableProviders = AvailableProviders ?? Array.Empty<ProviderName>();
        this.ArtistId = ArtistId;
        this.AlbumId = AlbumId;
        this.ReleaseDate = ReleaseDate;
    }

    public MusicCatalogId MusicCatalogId { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? AlbumTitle { get; init; }
    public string? Isrc { get; init; }
    public string? Mbid { get; init; }
    public int? DurationMs { get; init; }
    public bool IsPlayable { get; init; }
    public IReadOnlyList<ProviderName> AvailableProviders { get; init; }
    public ArtistId? ArtistId { get; init; }
    public AlbumId? AlbumId { get; init; }
    public DateOnly? ReleaseDate { get; init; }

    public bool Equals(LocalMusicTrackSearchResult? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return MusicCatalogId == other.MusicCatalogId
               && Title == other.Title
               && Artist == other.Artist
               && AlbumTitle == other.AlbumTitle
               && Isrc == other.Isrc
               && Mbid == other.Mbid
               && DurationMs == other.DurationMs
               && IsPlayable == other.IsPlayable
               && AvailableProviders.SequenceEqual(other.AvailableProviders)
               && ArtistId == other.ArtistId
               && AlbumId == other.AlbumId
               && ReleaseDate == other.ReleaseDate;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MusicCatalogId);
        hash.Add(Title);
        hash.Add(Artist);
        hash.Add(AlbumTitle);
        hash.Add(Isrc);
        hash.Add(Mbid);
        hash.Add(DurationMs);
        hash.Add(IsPlayable);
        foreach (var provider in AvailableProviders)
        {
            hash.Add(provider);
        }

        hash.Add(ArtistId);
        hash.Add(AlbumId);
        hash.Add(ReleaseDate);
        return hash.ToHashCode();
    }

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
