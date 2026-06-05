namespace Soundtrail.Services.Enrichment.Shared.Execution;

public sealed class TrackEnrichmentState
{
    public SongMetadata? CanonicalMetadata { get; private set; }

    public ProviderReference? MusicBrainz { get; private set; }

    public ProviderReference? Apple { get; private set; }

    public ProviderReference? YouTubeMusic { get; private set; }

    public bool IsPlayable =>
        CanonicalMetadata is not null
        && (Apple?.Confidence == ReferenceConfidence.Verified
            || YouTubeMusic?.Confidence == ReferenceConfidence.Verified);

    public void ApplyCanonicalMetadata(SongMetadata metadata) => CanonicalMetadata = metadata;

    public TrackEnrichmentState Copy()
    {
        var copy = new TrackEnrichmentState();

        if (CanonicalMetadata is not null)
        {
            copy.ApplyCanonicalMetadata(CanonicalMetadata);
        }

        ApplyCopy(MusicBrainz, copy);
        ApplyCopy(Apple, copy);
        ApplyCopy(YouTubeMusic, copy);

        return copy;
    }

    public void ApplyReference(
        ProviderName provider,
        Uri url,
        string? externalId,
        ReferenceConfidence confidence,
        ProviderName sourceProvider)
    {
        var candidate = new ProviderReference(provider, url, externalId, confidence, sourceProvider);

        switch (provider)
        {
            case ProviderName.MusicBrainz:
                MusicBrainz = ChooseReference(MusicBrainz, candidate);
                break;
            case ProviderName.Apple:
                Apple = ChooseReference(Apple, candidate);
                break;
            case ProviderName.YouTubeMusic:
                YouTubeMusic = ChooseReference(YouTubeMusic, candidate);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider.");
        }
    }

    private static ProviderReference ChooseReference(
        ProviderReference? current,
        ProviderReference candidate)
    {
        if (current is null)
        {
            return candidate;
        }

        if (candidate.Confidence > current.Confidence)
        {
            return candidate;
        }

        return current;
    }

    private static void ApplyCopy(
        ProviderReference? reference,
        TrackEnrichmentState state)
    {
        if (reference is null)
        {
            return;
        }

        state.ApplyReference(
            reference.Provider,
            reference.Url,
            reference.ExternalId,
            reference.Confidence,
            reference.SourceProvider);
    }
}
