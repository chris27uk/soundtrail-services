using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

public sealed class MusicTrackSearchMatchCollection(
    IReadOnlyList<MusicCatalogMatch> matches,
    DateOnly? releaseDate = null)
{
    private const decimal MinimumAcceptedScore = 0.80m;

    public IReadOnlyList<MusicCatalogMatch> Query(MusicSearchCriteria searchCriteria)
    {
        var normalizedQuery = searchCriteria.Query;
        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            var exactQueryMatches = matches
                .Where(match => MatchesExactQuery(match, normalizedQuery))
                .OrderByDescending(static match => match.Score)
                .ToArray();

            if (exactQueryMatches.Length > 0)
            {
                return exactQueryMatches;
            }
        }

        var exactIdentityMatches = matches
            .Where(static match => match.HasExactIdentityMatch())
            .OrderByDescending(static match => match.Score)
            .ToArray();

        if (releaseDate is not null)
        {
            var releaseDateMatches = exactIdentityMatches
                .Where(match => match.Evidence.ReleaseDate == releaseDate)
                .OrderByDescending(static match => match.Score)
                .ToArray();

            if (releaseDateMatches.Length > 0)
            {
                return releaseDateMatches;
            }
        }

        if (exactIdentityMatches.Length > 0)
        {
            return exactIdentityMatches;
        }

        return matches
            .Where(match => match.MeetsMinimumScore(MinimumAcceptedScore))
            .OrderByDescending(static match => match.Score)
            .ToArray();
    }

    public async Task<CatalogSearchFollowUp> DetermineFollowUpAsync(
        MusicSearchCriteria searchCriteria,
        PlaybackProviderFilter playback,
        ILocalMusicTrackSearch localMusicTrackSearch,
        CancellationToken cancellationToken)
    {
        var selectedMatches = Query(searchCriteria);
        if (selectedMatches.Count == 0)
        {
            return CatalogSearchFollowUp.TrackMetadataRequired();
        }

        var lookups = new List<StreamingLocationLookupCandidate>();
        foreach (var selectedMatch in selectedMatches)
        {
            var localTrack = await localMusicTrackSearch.GetByMusicCatalogIdAsync(selectedMatch.MusicCatalogId, cancellationToken);
            var lookup = LocalMusicTrackStreamingLocationLookup.CreateIfRequired(localTrack, playback);
            if (lookup is null)
            {
                continue;
            }

            lookups.Add(lookup);
        }

        return CatalogSearchFollowUp.StreamingLocationsRequired(lookups);
    }

    private static bool MatchesExactQuery(MusicCatalogMatch match, string normalizedQuery)
    {
        var evidence = match.Evidence;
        if (string.IsNullOrWhiteSpace(evidence.NormalizedTitle)
            || string.IsNullOrWhiteSpace(evidence.NormalizedArtist))
        {
            return false;
        }

        var titleArtist = MusicIdentityText.NormalizeFreeText($"{evidence.NormalizedTitle} {evidence.NormalizedArtist}");
        if (string.Equals(titleArtist, normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(evidence.NormalizedAlbumTitle))
        {
            return false;
        }

        var titleArtistAlbum = MusicIdentityText.NormalizeFreeText(
            $"{evidence.NormalizedTitle} {evidence.NormalizedArtist} {evidence.NormalizedAlbumTitle}");
        return string.Equals(titleArtistAlbum, normalizedQuery, StringComparison.Ordinal);
    }
}
