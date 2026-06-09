using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Services.Enrichment.Features.Execution.ApplyEnrichmentResponse;

internal static class MusicTrackFactBuilder
{
    public static IReadOnlyList<MusicTrackFact> Build(
        MusicTrackStream stream,
        EnrichmentResponse response)
    {
        var state = MusicTrackState.From(stream);
        var facts = new List<MusicTrackFact>();

        foreach (var fact in DiscoverFacts(state, response))
        {
            facts.Add(fact);
            state.Apply(fact);
        }

        facts.AddRange(DetermineBusinessIntentEvents(state, response));
        return facts;
    }

    private static IReadOnlyList<MusicTrackFact> DiscoverFacts(
        MusicTrackState state,
        EnrichmentResponse response)
    {
        var facts = new List<MusicTrackFact>();
        var sourceProvider = response.SourceProvider;

        if (sourceProvider == ProviderName.MusicBrainz
            && response.Metadata is not null
            && state.ShouldRecordMinimalInfo(response.Metadata))
        {
            facts.Add(new MinimalTrackInfoDiscovered(
                response.Metadata.Title,
                response.Metadata.Artist,
                response.Metadata.DurationMs,
                response.Metadata.Isrc,
                response.Metadata.Mbid,
                sourceProvider,
                response.CreatedAt));
        }

        foreach (var reference in response.References)
        {
            var current = state.GetReference(reference.Provider);
            if (!ShouldRecordResolvedReference(current, reference))
            {
                continue;
            }

            facts.Add(new ProviderPlaybackReferenceResolved(
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                sourceProvider,
                response.CreatedAt));
        }

        return facts;
    }

    private static IReadOnlyList<MusicTrackFact> DetermineBusinessIntentEvents(
        MusicTrackState state,
        EnrichmentResponse response)
    {
        if (response.SourceProvider != ProviderName.MusicBrainz || !state.HasCanonicalMetadata())
        {
            return [];
        }

        var facts = new List<MusicTrackFact>();

        if (state.HasPlaybackReference() || state.PlaybackReferencesResolutionRequired)
        {
            return facts;
        }

        var lookupKey = BuildPlaybackReferenceLookupKey(response);
        if (lookupKey is null)
        {
            return facts;
        }

        facts.Add(new PlaybackReferencesResolutionRequired(
            response.MusicCatalogId,
            response.Priority,
            response.CorrelationId,
            response.SourceProvider,
            response.CreatedAt,
            lookupKey));

        return facts;
    }

    private static PlaybackReferenceLookupKey? BuildPlaybackReferenceLookupKey(EnrichmentResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Metadata?.Isrc))
        {
            return PlaybackReferenceLookupKey.ByIsrc(response.Metadata.Isrc);
        }

        if (!string.IsNullOrWhiteSpace(response.Metadata?.Title)
            && !string.IsNullOrWhiteSpace(response.Metadata.Artist))
        {
            return PlaybackReferenceLookupKey.ByTrackNameAndArtist(response.Metadata.Title, response.Metadata.Artist);
        }

        return null;
    }

    private static bool ShouldRecordResolvedReference(
        ProviderReference? current,
        ExternalReference reference)
    {
        if (current is null)
        {
            return true;
        }

        return current.ExternalId != reference.ExternalId
            || current.Url != reference.Url;
    }

    private sealed class MusicTrackState
    {
        private string? title;
        private string? artist;
        private int? durationMs;
        private string? isrc;
        private string? mbid;

        public ProviderReference? Apple { get; private set; }

        public ProviderReference? YouTubeMusic { get; private set; }

        public ProviderReference? Spotify { get; private set; }

        public bool PlaybackReferencesResolutionRequired { get; private set; }

        public static MusicTrackState From(MusicTrackStream stream)
        {
            var state = new MusicTrackState();
            foreach (var fact in stream.Facts)
            {
                state.Apply(fact);
            }

            return state;
        }

        public void Apply(MusicTrackFact fact)
        {
            switch (fact)
            {
                case MinimalTrackInfoDiscovered minimalTrackInfoDiscovered:
                    title = minimalTrackInfoDiscovered.Title;
                    artist = minimalTrackInfoDiscovered.Artist;
                    durationMs = minimalTrackInfoDiscovered.DurationMs;
                    isrc = minimalTrackInfoDiscovered.Isrc;
                    mbid = minimalTrackInfoDiscovered.Mbid;
                    break;
                case ProviderPlaybackReferenceResolved providerPlaybackReferenceResolved:
                    var reference = new ProviderReference(
                        providerPlaybackReferenceResolved.Provider,
                        providerPlaybackReferenceResolved.Url,
                        providerPlaybackReferenceResolved.ExternalId,
                        ReferenceConfidence.Verified,
                        providerPlaybackReferenceResolved.SourceProvider);

                    if (providerPlaybackReferenceResolved.Provider == ProviderName.AppleMusic)
                    {
                        Apple = reference;
                    }
                    else if (providerPlaybackReferenceResolved.Provider == ProviderName.YoutubeMusic)
                    {
                        YouTubeMusic = reference;
                    }
                    else if (providerPlaybackReferenceResolved.Provider == ProviderName.Spotify)
                    {
                        Spotify = reference;
                    }

                    PlaybackReferencesResolutionRequired = false;

                    break;
                case PlaybackReferencesResolutionRequired _:
                    PlaybackReferencesResolutionRequired = true;
                    break;
            }
        }

        public bool HasCanonicalMetadata() =>
            !string.IsNullOrWhiteSpace(title)
            && !string.IsNullOrWhiteSpace(artist);

        public bool HasPlaybackReference() =>
            Apple is not null || YouTubeMusic is not null || Spotify is not null;

        public bool ShouldRecordMinimalInfo(SongMetadata metadata)
        {
            if (!HasCanonicalMetadata())
            {
                return true;
            }

            return title != metadata.Title
                || artist != metadata.Artist
                || durationMs != metadata.DurationMs
                || !string.Equals(isrc, metadata.Isrc, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(mbid, metadata.Mbid, StringComparison.OrdinalIgnoreCase);
        }

        public ProviderReference? GetReference(ProviderName provider) =>
            provider switch
            {
                _ when provider == ProviderName.AppleMusic => Apple,
                _ when provider == ProviderName.YoutubeMusic => YouTubeMusic,
                _ when provider == ProviderName.Spotify => Spotify,
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
            };
    }
}
