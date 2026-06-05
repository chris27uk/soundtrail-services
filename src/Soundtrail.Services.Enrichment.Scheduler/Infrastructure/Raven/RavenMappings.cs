using Soundtrail.Services.Enrichment.Shared.Persistence;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven.Documents;

namespace Soundtrail.Services.Enrichment.Scheduler.Infrastructure.Raven;

internal static class RavenMappings
{
    public static RankedMusicCandidate ToDomain(this RavenRankedMusicCandidateDocument document) =>
        new(
            MusicCatalogId.From(document.MusicCatalogId),
            document.RequestCount,
            document.HighestTrustLevelSeen,
            document.RiskScore,
            Enum.Parse<RankedMusicCandidateStatus>(document.Status, ignoreCase: true),
            document.NextEligibleAt);

    public static RavenRankedMusicCandidateDocument ToDocument(this RankedMusicCandidate candidate) =>
        new()
        {
            Id = RavenRankedMusicCandidateDocument.GetDocumentId(candidate.MusicCatalogId.Value),
            MusicCatalogId = candidate.MusicCatalogId.Value,
            RequestCount = candidate.RequestCount,
            HighestTrustLevelSeen = candidate.HighestTrustLevelSeen,
            RiskScore = candidate.RiskScore,
            Status = candidate.Status.ToString(),
            NextEligibleAt = candidate.NextEligibleAt
        };

    public static TrackEnrichmentState ToDomain(this RavenTrackDocument document)
    {
        var state = new TrackEnrichmentState();

        if (document.CanonicalMetadata is not null)
        {
            state.ApplyCanonicalMetadata(new SongMetadata(
                document.CanonicalMetadata.Title,
                document.CanonicalMetadata.Artist,
                document.CanonicalMetadata.Isrc,
                document.CanonicalMetadata.Mbid,
                document.CanonicalMetadata.DurationMs));
        }

        ApplyReference(document.MusicBrainzReference, state);
        ApplyReference(document.AppleReference, state);
        ApplyReference(document.YouTubeMusicReference, state);

        return state;
    }

    public static void Apply(this RavenTrackDocument document, TrackEnrichmentState state)
    {
        document.CanonicalMetadata = state.CanonicalMetadata is null
            ? null
            : new RavenSongMetadataDocument
            {
                Title = state.CanonicalMetadata.Title,
                Artist = state.CanonicalMetadata.Artist,
                Isrc = state.CanonicalMetadata.Isrc,
                Mbid = state.CanonicalMetadata.Mbid,
                DurationMs = state.CanonicalMetadata.DurationMs
            };

        document.MusicBrainzReference = ToDocument(state.MusicBrainz);
        document.AppleReference = ToDocument(state.Apple);
        document.YouTubeMusicReference = ToDocument(state.YouTubeMusic);

        document.Title = state.CanonicalMetadata?.Title ?? document.Title;
        document.Artist = state.CanonicalMetadata?.Artist ?? document.Artist;
        document.Isrc = state.CanonicalMetadata?.Isrc ?? document.Isrc;
        document.Mbid = state.CanonicalMetadata?.Mbid ?? document.Mbid;
        document.DurationMs = state.CanonicalMetadata?.DurationMs ?? document.DurationMs;
        document.AppleId = state.Apple?.ExternalId ?? document.AppleId;
        document.SearchText = RavenTrackDocument.BuildSearchText(document.Title, document.Artist);
    }

    private static void ApplyReference(
        RavenProviderReferenceDocument? reference,
        TrackEnrichmentState state)
    {
        if (reference is null)
        {
            return;
        }

        state.ApplyReference(
            Enum.Parse<ProviderName>(reference.Provider, ignoreCase: true),
            new Uri(reference.Url),
            reference.ExternalId,
            Enum.Parse<ReferenceConfidence>(reference.Confidence, ignoreCase: true),
            Enum.Parse<ProviderName>(reference.SourceProvider, ignoreCase: true));
    }

    private static RavenProviderReferenceDocument? ToDocument(ProviderReference? reference)
    {
        if (reference is null)
        {
            return null;
        }

        return new RavenProviderReferenceDocument
        {
            Provider = reference.Provider.ToString(),
            Url = reference.Url.ToString(),
            ExternalId = reference.ExternalId,
            Confidence = reference.Confidence.ToString(),
            SourceProvider = reference.SourceProvider.ToString()
        };
    }
}
