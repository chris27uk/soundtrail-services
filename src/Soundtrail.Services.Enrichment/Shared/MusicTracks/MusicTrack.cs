using Soundtrail.Services.Enrichment.Shared.EventSourcing;
using Soundtrail.Services.Enrichment.Shared.Execution;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Shared;

namespace Soundtrail.Services.Enrichment.Shared.MusicTracks;

public sealed class MusicTrack
{
    private readonly EventHandlers eventHandlers;
    private readonly List<MusicTrackFact> uncommittedEvents = [];
    private int version;
    private string? title;
    private string? artist;
    private int? durationMs;
    private string? isrc;
    private string? mbid;
    private readonly MusicCatalogId id;
    private ProviderReference? apple;
    private ProviderReference? youTubeMusic;
    private bool appleMusicResolutionRequired;
    private bool youTubeMusicResolutionRequired;

    private MusicTrack(MusicCatalogId id, int version, IEnumerable<MusicTrackFact> events)
    {
        this.id = id;
        this.version = version;
        this.eventHandlers = this.BindEventHandlers();
        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    private EventHandlers BindEventHandlers()
    {
        var handlers = new EventHandlers();
        handlers.Register<MinimalTrackInfoDiscovered>(this.Apply);
        handlers.Register<ProviderPlaybackReferenceResolved>(this.Apply);
        handlers.Register<AppleMusicResolutionRequired>(this.Apply);
        handlers.Register<YouTubeMusicResolutionRequired>(this.Apply);
        handlers.Register<TrackLinkedToAlbum>(this.Apply);
        handlers.Register<TrackLinkedToArtist>(this.Apply);
        return handlers;
    }

    public static async Task<MusicTrack> LoadAsync(
        MusicCatalogId musicCatalogId,
        IMusicTrackEventRepository repository,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadEventsAsync(musicCatalogId, cancellationToken);
        return new MusicTrack(musicCatalogId, stream.Version, stream.Facts);
    }

    public async Task<AppendMusicTrackStreamResult> SaveAsync(
        IMusicTrackEventRepository repository,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var result = await repository.AppendEventsAsync(
            this.id,
            version,
            commandId,
            uncommittedEvents,
            cancellationToken);

        if (result.Appended)
        {
            version = result.Version;
            uncommittedEvents.Clear();
        }

        return result;
    }

    public void Record(EnrichmentResponse response)
    {
        foreach (var fact in DiscoverFacts(response))
        {
            Apply(fact, isNew: true);
        }
        
        foreach (var fact in DetermineBusinessIntentFacts(response))
        {
            Apply(fact, isNew: true);
        }
    }

    private IReadOnlyList<MusicTrackFact> DiscoverFacts(EnrichmentResponse response)
    {
        var facts = new List<MusicTrackFact>();

        if (response.SourceProvider == ProviderName.MusicBrainz && response.Metadata is not null)
        {
            if (ShouldRecordMinimalInfo(response.Metadata))
            {
                facts.Add(new MinimalTrackInfoDiscovered(
                    response.Metadata.Title,
                    response.Metadata.Artist,
                    response.Metadata.DurationMs,
                    response.Metadata.Isrc,
                    response.Metadata.Mbid,
                    response.SourceProvider,
                    response.CreatedAt));
            }
        }

        foreach (var reference in response.References)
        {
            if (response.SourceProvider != reference.Provider)
            {
                continue;
            }

            var current = GetReference(reference.Provider);
            if (ShouldRecordResolvedReference(current, reference))
            {
                facts.Add(new ProviderPlaybackReferenceResolved(
                    reference.Provider,
                    reference.ExternalId,
                    reference.Url,
                    response.SourceProvider,
                    response.CreatedAt));
            }
        }

        return facts;
    }

    public void Project(IMusicTrackProjectionWriter writer)
    {
        if (!HasCanonicalMetadata())
        {
            return;
        }

        writer.WriteCanonicalMetadata(title!, artist!, isrc, mbid, durationMs);

        if (apple is not null)
        {
            writer.WriteProviderReference(
                apple.Provider,
                apple.Url,
                apple.ExternalId,
                apple.Confidence,
                apple.SourceProvider);
        }

        if (youTubeMusic is not null)
        {
            writer.WriteProviderReference(
                youTubeMusic.Provider,
                youTubeMusic.Url,
                youTubeMusic.ExternalId,
                youTubeMusic.Confidence,
                youTubeMusic.SourceProvider);
        }

        writer.WritePlayable(IsPlayable());
    }

    private void Apply(MusicTrackFact @event, bool isNew)
    {
        this.eventHandlers.Handle(@event);
        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private void Apply(MinimalTrackInfoDiscovered @event)
    {
        title = @event.Title;
        artist = @event.Artist;
        durationMs = @event.DurationMs;
        isrc = @event.Isrc;
        mbid = @event.Mbid;
    }

    private void Apply(ProviderPlaybackReferenceResolved @event)
    {
        ApplyReference(
            @event.Provider,
            @event.Url,
            @event.ExternalId,
            ReferenceConfidence.Verified,
            @event.SourceProvider);
    }

    private void Apply(AppleMusicResolutionRequired _)
    {
        appleMusicResolutionRequired = true;
    }

    private void Apply(YouTubeMusicResolutionRequired _)
    {
        youTubeMusicResolutionRequired = true;
    }

    private void Apply(TrackLinkedToAlbum _)
    {
    }

    private void Apply(TrackLinkedToArtist _)
    {
    }

    private bool ShouldRecordMinimalInfo(SongMetadata metadata)
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

    private ProviderReference? GetReference(ProviderName provider) =>
        provider switch
        {
            ProviderName.Apple => apple,
            ProviderName.YouTubeMusic => youTubeMusic,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };

    private void ApplyReference(
        ProviderName provider,
        Uri url,
        string? externalId,
        ReferenceConfidence confidence,
        ProviderName sourceProvider)
    {
        var candidate = new ProviderReference(provider, url, externalId, confidence, sourceProvider);

        switch (provider)
        {
            case ProviderName.Apple:
                apple = candidate;
                appleMusicResolutionRequired = false;
                break;
            case ProviderName.YouTubeMusic:
                youTubeMusic = candidate;
                youTubeMusicResolutionRequired = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider.");
        }
    }

    private IReadOnlyList<MusicTrackFact> DetermineBusinessIntentFacts(EnrichmentResponse response)
    {
        if (response.SourceProvider != ProviderName.MusicBrainz || !HasCanonicalMetadata())
        {
            return [];
        }

        var facts = new List<MusicTrackFact>();

        if (apple is null && !appleMusicResolutionRequired)
        {
            facts.Add(new AppleMusicResolutionRequired(
                this.id,
                response.Priority,
                response.CorrelationId,
                response.SourceProvider,
                response.CreatedAt));
        }

        if (youTubeMusic is null && !youTubeMusicResolutionRequired)
        {
            facts.Add(new YouTubeMusicResolutionRequired(
                this.id,
                response.Priority,
                response.CorrelationId,
                response.SourceProvider,
                response.CreatedAt));
        }

        return facts;
    }

    private bool HasCanonicalMetadata() =>
        !string.IsNullOrWhiteSpace(title)
        && !string.IsNullOrWhiteSpace(artist);

    private bool IsPlayable() =>
        HasCanonicalMetadata()
        && (apple is not null || youTubeMusic is not null);
}
