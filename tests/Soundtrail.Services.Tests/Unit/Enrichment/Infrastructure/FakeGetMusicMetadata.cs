using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

public sealed class FakeGetMusicMetadata : IGetTrackMetadata
{
    private readonly Dictionary<string, SongMetadata> byQuery = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SongMetadata> byIsrc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SongMetadata> byNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> ambiguousQueries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> ambiguousIsrc = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> ambiguousNames = new(StringComparer.OrdinalIgnoreCase);
    private Exception? exception;

    public List<string> Lookups { get; } = [];

    public Task<SongMetadata?> GetMetadataAsync(
        MusicSearchCriteria searchCriteria,
        CancellationToken cancellationToken)
    {
        if (exception is not null)
        {
            throw exception;
        }

        return searchCriteria.Match(
            query =>
            {
                var key = MusicMetadataLookupMatch.Normalize(query);
                Lookups.Add($"query:{key}");

                if (ambiguousQueries.Contains(key))
                {
                    return Task.FromResult<SongMetadata?>(null);
                }

                byQuery.TryGetValue(key, out var queryMatch);
                return Task.FromResult(queryMatch);
            },
            (track, artist, album) =>
            {
                var key = Key(track, artist, album);
                Lookups.Add($"names:{key}");

                if (ambiguousNames.Contains(key))
                {
                    return Task.FromResult<SongMetadata?>(null);
                }

                byNames.TryGetValue(key, out var nameMatch);
                return Task.FromResult(nameMatch);
            },
            isrc =>
            {
                Lookups.Add($"isrc:{isrc}");

                if (ambiguousIsrc.Contains(isrc))
                {
                    return Task.FromResult<SongMetadata?>(null);
                }

                this.byIsrc.TryGetValue(isrc, out var isrcMatch);
                return Task.FromResult(isrcMatch);
            });
    }

    public void SeedQuery(string query, SongMetadata metadata) => byQuery[MusicMetadataLookupMatch.Normalize(query)] = metadata;

    public void SeedIsrc(string isrc, SongMetadata metadata) => byIsrc[MusicIdentityText.NormalizeCompact(isrc)] = metadata;

    public void SeedNames(string title, string artist, string? albumName, SongMetadata metadata) =>
        byNames[Key(title, artist, albumName)] = metadata;

    public void SeedAmbiguousQuery(string query) => ambiguousQueries.Add(MusicMetadataLookupMatch.Normalize(query));

    public void SeedAmbiguousIsrc(string isrc) => ambiguousIsrc.Add(MusicIdentityText.NormalizeCompact(isrc));

    public void SeedAmbiguousNames(string title, string artist, string? albumName) =>
        ambiguousNames.Add(Key(title, artist, albumName));

    public void Throw(Exception ex) => exception = ex;

    private static string Key(string title, string artist, string? albumName) =>
        $"{MusicMetadataLookupMatch.Normalize(title)}::{MusicMetadataLookupMatch.Normalize(artist)}::{MusicMetadataLookupMatch.Normalize(albumName)}";
}
