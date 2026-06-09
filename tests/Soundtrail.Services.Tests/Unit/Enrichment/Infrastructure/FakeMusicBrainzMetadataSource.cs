using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.MusicBrainzLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.TrackLookup;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class FakeMusicBrainzMetadataSource : IMusicBrainzMetadataSource
{
    private readonly Dictionary<string, SongMetadata> byIsrc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SongMetadata> byNames = new(StringComparer.OrdinalIgnoreCase);

    public List<string> Lookups { get; } = [];

    public Task<SongMetadata?> GetMetadataAsync(
        CanonicalMusicMetadataLookup lookup,
        CancellationToken cancellationToken)
    {
        if (lookup is CanonicalMusicMetadataLookup.ByIsrc byIsrc)
        {
            Lookups.Add($"isrc:{byIsrc.Isrc}");
            this.byIsrc.TryGetValue(byIsrc.Isrc, out var isrcMatch);
            return Task.FromResult(isrcMatch);
        }

        if (lookup is CanonicalMusicMetadataLookup.ByTrackNameArtistAndAlbum byNamesLookup)
        {
            var key = Key(byNamesLookup.TrackName, byNamesLookup.ArtistName, byNamesLookup.AlbumName);
            Lookups.Add($"names:{key}");
            byNames.TryGetValue(key, out var nameMatch);
            return Task.FromResult(nameMatch);
        }

        return Task.FromResult<SongMetadata?>(null);
    }

    public void SeedIsrc(string isrc, SongMetadata metadata) => byIsrc[isrc] = metadata;

    public void SeedNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        byNames[Key(title, artist, albumName)] = metadata;

    private static string Key(string title, string artist, string? albumName) =>
        $"{MusicMetadataLookupMatch.Normalize(title)}::{MusicMetadataLookupMatch.Normalize(artist)}::{MusicMetadataLookupMatch.Normalize(albumName)}";
}
