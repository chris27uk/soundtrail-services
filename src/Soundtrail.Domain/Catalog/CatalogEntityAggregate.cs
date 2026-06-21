using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;

namespace Soundtrail.Domain.Catalog;

public sealed class CatalogEntityAggregate
{
    private readonly EventHandlers<CatalogEntityAggregate> eventHandlers;
    private readonly List<IMusicTrackEvent> uncommittedEvents = [];
    private readonly HashSet<string> discoveredReferenceProviders = [];
    private MusicCatalogId? musicCatalogId;
    private bool playbackReferencesResolutionRequired;
    private int version;

    private CatalogEntityAggregate(
        MusicCatalogId musicCatalogId,
        IEnumerable<IMusicTrackEvent> events,
        int version)
    {
        this.musicCatalogId = musicCatalogId;
        this.version = version;
        eventHandlers = CreateHandlers();

        foreach (var @event in events)
        {
            Apply(@event, isNew: false);
        }
    }

    public static async Task<CatalogEntityAggregate> LoadAsync(
        IMusicTrackEventRepository repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadEventsAsync(musicCatalogId, cancellationToken);
        return new CatalogEntityAggregate(musicCatalogId, stream.Events, stream.Version);
    }

    public void RecordEnrichmentResponse(EnrichmentResponse response)
    {
        if (musicCatalogId is null || musicCatalogId != response.MusicCatalogId)
        {
            throw new InvalidOperationException("Aggregate music catalog id does not match the enrichment response.");
        }

        if (response.SourceProvider == ProviderName.MusicBrainz && response.Metadata is not null)
        {
            if (response.Hierarchy?.ArtistId is not null || !string.IsNullOrWhiteSpace(response.Metadata.Artist))
            {
                Apply(new ArtistDiscovered(
                    response.Hierarchy?.ArtistId?.Value,
                    response.Metadata.Artist,
                    response.Metadata.SourceArtistId,
                    response.SourceProvider,
                    response.CreatedAt), isNew: true);
            }

            if (response.Hierarchy?.AlbumId is not null)
            {
                Apply(new AlbumDiscovered(
                    response.Hierarchy.AlbumId!.Value,
                    response.Metadata.AlbumTitle,
                    response.Metadata.SourceAlbumId,
                    response.Metadata.ReleaseDate,
                    response.SourceProvider,
                    response.CreatedAt), isNew: true);
            }

            Apply(new TrackDiscovered(
                response.Metadata.Title,
                response.Metadata.Artist,
                response.Metadata.DurationMs,
                response.Metadata.Isrc,
                response.Metadata.Mbid,
                response.SourceProvider,
                response.CreatedAt), isNew: true);
        }

        foreach (var reference in response.References)
        {
            Apply(new ProviderReferenceDiscovered(
                reference.Provider,
                reference.ExternalId,
                reference.Url,
                response.SourceProvider,
                response.CreatedAt), isNew: true);
        }

        foreach (var failedProvider in response.FailedProviders)
        {
            Apply(new ProviderReferenceLookupFailed(
                failedProvider.Provider,
                failedProvider.SourceProvider,
                response.CreatedAt), isNew: true);
        }

        if (ShouldRequirePlaybackReferencesResolution(response))
        {
            var searchTerm = !string.IsNullOrWhiteSpace(response.Metadata!.Isrc)
                ? MusicSearchTerm.ByIsrc(response.Metadata.Isrc)
                : MusicSearchTerm.ByTrackArtistAlbum(
                    response.Metadata.Title,
                    response.Metadata.Artist,
                    album: null);

            Apply(new PlaybackReferencesResolutionRequired(
                response.MusicCatalogId,
                response.Priority,
                response.CorrelationId,
                response.SourceProvider,
                response.CreatedAt,
                searchTerm,
                response.Hierarchy), isNew: true);
        }
    }

    public Task<AppendMusicTrackStreamResult> SaveAsync(
        IMusicTrackEventRepository repository,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        if (uncommittedEvents.Count == 0)
        {
            return Task.FromResult(new AppendMusicTrackStreamResult(true, version, []));
        }

        return SaveInternalAsync(repository, commandId, cancellationToken);
    }

    private async Task<AppendMusicTrackStreamResult> SaveInternalAsync(
        IMusicTrackEventRepository repository,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var append = await repository.AppendEventsAsync(
            RequireMusicCatalogId(),
            version,
            commandId,
            uncommittedEvents.AsReadOnly(),
            cancellationToken);

        if (append.Appended)
        {
            version = append.Version;
            uncommittedEvents.Clear();
        }

        return append;
    }

    private bool ShouldRequirePlaybackReferencesResolution(EnrichmentResponse response) =>
        response.SourceProvider == ProviderName.MusicBrainz
        && response.Metadata is not null
        && discoveredReferenceProviders.Count == 0
        && !playbackReferencesResolutionRequired;

    private void Apply(IMusicTrackEvent @event, bool isNew)
    {
        eventHandlers.Handle(@event);

        if (isNew)
        {
            uncommittedEvents.Add(@event);
        }
    }

    private MusicCatalogId RequireMusicCatalogId() =>
        musicCatalogId ?? throw new InvalidOperationException("Aggregate music catalog id has not been established.");

    private EventHandlers<CatalogEntityAggregate> CreateHandlers()
    {
        var handlers = new EventHandlers<CatalogEntityAggregate>();
        handlers.Register<PlaybackReferencesResolutionRequired>(_ => playbackReferencesResolutionRequired = true);
        handlers.Register<ProviderReferenceDiscovered>(@event => discoveredReferenceProviders.Add(@event.Provider.Value));
        handlers.Register<TrackDiscovered>(_ => { });
        handlers.Register<ArtistDiscovered>(_ => { });
        handlers.Register<AlbumDiscovered>(_ => { });
        handlers.Register<ProviderReferenceLookupFailed>(_ => { });
        handlers.Register<ArtworkDiscovered>(_ => { });
        handlers.Register<MetadataCorrected>(_ => { });
        return handlers;
    }
}
