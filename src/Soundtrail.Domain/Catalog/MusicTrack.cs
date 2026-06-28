using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Catalog;

public sealed class MusicTrack
{
    private readonly EventHandlers<MusicTrack> eventHandlers;
    private readonly List<IMusicTrackEvent> uncommittedEvents = [];
    private readonly HashSet<string> discoveredReferenceProviders = [];
    private MusicCatalogId? musicCatalogId;
    private bool streamingLocationsRequired;
    private int version;

    private MusicTrack(
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

    public static async Task<MusicTrack> LoadAsync(
        IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> repository,
        MusicCatalogId musicCatalogId,
        CancellationToken cancellationToken)
    {
        var stream = await repository.LoadAsync(musicCatalogId, cancellationToken);
        return new MusicTrack(musicCatalogId, stream.Events, stream.Version);
    }

    public void MetadataFetched(MusicCatalogMetadataFetched response)
    {
        if (musicCatalogId is null || musicCatalogId != response.MusicCatalogId)
        {
            throw new InvalidOperationException("Aggregate music catalog id does not match the enrichment response.");
        }

        if (response.SourceProvider == LookupSource.MusicBrainz && response.Metadata is not null)
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

        if (ShouldRequireStreamingLocations(response))
        {
            var searchTerm = !string.IsNullOrWhiteSpace(response.Metadata!.Isrc)
                ? MusicSearchCriteria.ByIsrc(response.Metadata.Isrc)
                : MusicSearchCriteria.ByTrackArtistAlbum(
                    response.Metadata.Title,
                    response.Metadata.Artist,
                    album: null);

            Apply(new StreamingLocationsRequired(
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
        IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> repository,
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
        IEventStreamRepository<MusicCatalogId, IMusicTrackEvent> repository,
        CommandId commandId,
        CancellationToken cancellationToken)
    {
        var append = await repository.AppendAsync(
            new AppendRequest<MusicCatalogId, IMusicTrackEvent>(
                RequireMusicCatalogId(),
                version,
                uncommittedEvents.AsReadOnly(),
                OperationId.From(commandId.Value)),
            cancellationToken);

        if (append.Outcome == AppendOutcome.VersionMismatch)
        {
            throw new MusicTrackStreamConcurrencyException(RequireMusicCatalogId(), version, append.Version);
        }

        if (append.Appended)
        {
            version = append.Version;
            uncommittedEvents.Clear();
        }

        return append.Outcome == AppendOutcome.DuplicateOperation
            ? new AppendMusicTrackStreamResult(false, append.Version, [])
            : new AppendMusicTrackStreamResult(true, append.Version, append.Events);
    }

    private bool ShouldRequireStreamingLocations(MusicCatalogMetadataFetched response) =>
        response.SourceProvider == LookupSource.MusicBrainz
        && response.Metadata is not null
        && discoveredReferenceProviders.Count == 0
        && !streamingLocationsRequired;

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

    private EventHandlers<MusicTrack> CreateHandlers()
    {
        var handlers = new EventHandlers<MusicTrack>();
        handlers.Register<StreamingLocationsRequired>(_ => streamingLocationsRequired = true);
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
