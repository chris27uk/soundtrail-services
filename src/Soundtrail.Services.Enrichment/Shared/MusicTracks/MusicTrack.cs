using Soundtrail.Contracts;
using Soundtrail.Contracts.Worker.Responses;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.EventSourcing;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Execution;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.Search;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.MusicTracks;

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
            this.version,
            commandId,
            this.uncommittedEvents,
            cancellationToken);

        if (result.Appended)
        {
            this.version = result.Version;
            this.uncommittedEvents.Clear();
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
        var sourceProvider = response.SourceProvider;

        if (sourceProvider == ProviderName.MusicBrainz && response.Metadata is not null)
        {
            if (ShouldRecordMinimalInfo(response.Metadata))
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
        }

        foreach (var reference in response.References)
        {
            var provider = reference.Provider;
            if (sourceProvider != provider)
            {
                continue;
            }

            var current = GetReference(provider);
            if (ShouldRecordResolvedReference(current, reference))
            {
                facts.Add(new ProviderPlaybackReferenceResolved(
                    provider,
                    reference.ExternalId,
                    reference.Url,
                    sourceProvider,
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

        writer.WriteCanonicalMetadata(this.title!, this.artist!, this.isrc, this.mbid, this.durationMs);

        if (this.apple is not null)
        {
            writer.WriteProviderReference(
                this.apple.Provider,
                this.apple.Url,
                this.apple.ExternalId,
                this.apple.Confidence,
                this.apple.SourceProvider);
        }

        if (this.youTubeMusic is not null)
        {
            writer.WriteProviderReference(
                this.youTubeMusic.Provider,
                this.youTubeMusic.Url,
                this.youTubeMusic.ExternalId,
                this.youTubeMusic.Confidence,
                this.youTubeMusic.SourceProvider);
        }

        writer.WritePlayable(IsPlayable());
    }

    private void Apply(MusicTrackFact @event, bool isNew)
    {
        this.eventHandlers.Handle(@event);
        if (isNew)
        {
            this.uncommittedEvents.Add(@event);
        }
    }

    private void Apply(MinimalTrackInfoDiscovered @event)
    {
        this.title = @event.Title;
        this.artist = @event.Artist;
        this.durationMs = @event.DurationMs;
        this.isrc = @event.Isrc;
        this.mbid = @event.Mbid;
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
        this.appleMusicResolutionRequired = true;
    }

    private void Apply(YouTubeMusicResolutionRequired _)
    {
        this.youTubeMusicResolutionRequired = true;
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

        return this.title != metadata.Title
            || this.artist != metadata.Artist
            || this.durationMs != metadata.DurationMs
            || !string.Equals(this.isrc, metadata.Isrc, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(this.mbid, metadata.Mbid, StringComparison.OrdinalIgnoreCase);
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
            _ when provider == ProviderName.AppleMusic => this.apple,
            _ when provider == ProviderName.YoutubeMusic => this.youTubeMusic,
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
            case var _ when provider == ProviderName.AppleMusic:
                this.apple = candidate;
                this.appleMusicResolutionRequired = false;
                break;
            case var _ when provider == ProviderName.YoutubeMusic:
                this.youTubeMusic = candidate;
                this.youTubeMusicResolutionRequired = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider.");
        }
    }

    private IReadOnlyList<MusicTrackFact> DetermineBusinessIntentFacts(EnrichmentResponse response)
    {
        var sourceProvider = response.SourceProvider;
        if (sourceProvider != ProviderName.MusicBrainz || !HasCanonicalMetadata())
        {
            return [];
        }

        var facts = new List<MusicTrackFact>();

        if (this.apple is null && !this.appleMusicResolutionRequired)
        {
            facts.Add(new AppleMusicResolutionRequired(
                this.id,
                response.Priority,
                response.CorrelationId,
                sourceProvider,
                response.CreatedAt));
        }

        if (this.youTubeMusic is null && !this.youTubeMusicResolutionRequired)
        {
            facts.Add(new YouTubeMusicResolutionRequired(
                this.id,
                response.Priority,
                response.CorrelationId,
                sourceProvider,
                response.CreatedAt));
        }

        return facts;
    }

    private bool HasCanonicalMetadata() =>
        !string.IsNullOrWhiteSpace(this.title)
        && !string.IsNullOrWhiteSpace(this.artist);

    private bool IsPlayable() =>
        HasCanonicalMetadata()
        && (this.apple is not null || this.youTubeMusic is not null);
}
