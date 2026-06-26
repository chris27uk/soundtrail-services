using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class CatalogSearchRequestedHandler(
    IMusicCatalogCandidateSearch musicCatalogCandidateSearch,
    IRecordCatalogSearchStartedPort recordCatalogSearchStartedPort,
    ILocalMusicTrackSearch localMusicTrackSearch)
{
    private const decimal MinimumAcceptedScore = 0.80m;

    public async Task Handle(
        CatalogSearchAttempt request,
        CancellationToken cancellationToken = default)
    {
        var matches = await musicCatalogCandidateSearch.SearchAsync(request.Query, cancellationToken);
        var localTrackForResolution = await TryLoadResolutionTrackAsync(request.Criteria, cancellationToken);
        var selectedMatches = SelectMatches(matches, request.Query, localTrackForResolution?.ReleaseDate);
        if (selectedMatches.Count == 0)
        {
            return;
        }

        await recordCatalogSearchStartedPort.RecordAsync(
            request.Criteria,
            selectedMatches.Select(static match => match.MusicCatalogId).ToArray(),
            request.TrustLevel,
            request.RiskScore,
            request.OccurredAt,
            request.CorrelationId,
            cancellationToken);
    }

    private async Task<LocalMusicTrackSearchResult?> TryLoadResolutionTrackAsync(
        CatalogSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        const string trackPrefix = "track:";
        if (!criteria.Value.StartsWith(trackPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var trackId = criteria.Value[trackPrefix.Length..];
        return await localMusicTrackSearch.GetByMusicCatalogIdAsync(MusicCatalogId.From(trackId), cancellationToken);
    }

    private static IReadOnlyList<MusicCatalogMatch> SelectMatches(
        IReadOnlyList<MusicCatalogMatch> matches,
        NormalizedSearchQuery query,
        DateOnly? releaseDate)
    {
        var exactQueryMatches = matches
            .Where(match => MatchesExactQuery(match, query.Value))
            .OrderByDescending(static match => match.Score)
            .ToArray();

        if (exactQueryMatches.Length > 0)
        {
            return exactQueryMatches;
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
