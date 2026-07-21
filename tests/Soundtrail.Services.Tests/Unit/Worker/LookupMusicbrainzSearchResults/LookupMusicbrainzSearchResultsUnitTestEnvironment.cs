using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupMusicbrainzSearchResults;

internal sealed class LookupMusicbrainzSearchResultsUnitTestEnvironment
{
    private LookupMusicbrainzSearchResultsUnitTestEnvironment()
    {
        Clock = new ClockFake();
        CommandBus = new CommandBusFake();
        ReadCatalogEntriesBySearchCriteriaPort = new ReadCatalogEntriesBySearchCriteriaPortFake();
        AdmissionPort = new LookupExecutionAdmissionPortFake();
        ReceiptStore = new LookupExecutionReceiptStoreFake();
        InnerHandler = new InnerHandlerFake();
    }

    public ClockFake Clock { get; }

    public CommandBusFake CommandBus { get; }

    public ReadCatalogEntriesBySearchCriteriaPortFake ReadCatalogEntriesBySearchCriteriaPort { get; }

    public LookupExecutionAdmissionPortFake AdmissionPort { get; }

    public LookupExecutionReceiptStoreFake ReceiptStore { get; }

    public InnerHandlerFake InnerHandler { get; }

    public static LookupMusicbrainzSearchResultsUnitTestEnvironment Create() => new();

    public LookupMusicbrainzSearchResultsHandler CreateBusinessSubject() =>
        new(ReadCatalogEntriesBySearchCriteriaPort, Clock, CommandBus);

    public AdmittedLookupHandlerDecorator<LookupMusicbrainzSearchResultsMessage> CreateAdmissionSubject(
        IHandler<LookupMusicbrainzSearchResultsMessage>? inner = null) =>
        new(inner ?? InnerHandler, new LookupMusicbrainzSearchResultsDecoratorMetadata(), CommandBus, AdmissionPort, Clock);

    public IdempotentLookupHandlerDecorator<LookupMusicbrainzSearchResultsMessage> CreateIdempotencySubject(
        IHandler<LookupMusicbrainzSearchResultsMessage>? inner = null) =>
        new(inner ?? InnerHandler, new LookupMusicbrainzSearchResultsDecoratorMetadata(), ReceiptStore, CommandBus, Clock);

    public LookupMusicbrainzSearchResultsMessage CreateRequest(
        string commandId = "cmd-musicbrainz-search",
        string query = "rare song",
        SearchType searchType = SearchType.All,
        DateTimeOffset? createdAt = null) =>
        new(
            MessageId.For(commandId),
            CorrelationId.From($"corr:{commandId}"),
            createdAt ?? new DateTimeOffset(2026, 7, 20, 10, 30, 0, TimeSpan.Zero),
            LookupPriorityBand.High,
            new SearchCriteria(query, searchType));

    public static IReadOnlyList<CatalogDiscoveryEntry> CreateEntries()
    {
        var artistId = ArtistId.From("artist-mb-1");
        var albumId = Soundtrail.Domain.Catalog.Albums.AlbumId.From(artistId.Value, "release-mb-1");
        var trackId = TestTrackIds.Create("musicbrainz-search-track-1");
        var track = new Track(trackId)
        {
            Title = "Rare Unknown Song",
            ArtistName = "Test Artist",
            Isrc = "isrc-rare-1",
            UpdatedAt = new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)
        };

        return
        [
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicArtist(
                    new Artist
                    {
                        Id = artistId,
                        Name = ArtistName.From("Test Artist")
                    })),
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicAlbum(
                    new Soundtrail.Domain.Catalog.Albums.Album(
                        albumId,
                        "Rare Release",
                        "release-mb-1",
                        new DateOnly(2026, 1, 2),
                        artworkUrl: null,
                        updatedAt: new DateTimeOffset(2026, 7, 20, 11, 0, 0, TimeSpan.Zero)))),
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicTrack(track))
        ];
    }

    public sealed class ClockFake : IClockPort
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 7, 20, 11, 45, 0, TimeSpan.Zero);
    }

    public sealed class CommandBusFake : ICommandBus
    {
        public List<object> Messages { get; } = [];

        public Task SendAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    public sealed class ReadCatalogEntriesBySearchCriteriaPortFake : IReadCatalogEntriesBySearchCriteriaPort
    {
        public IReadOnlyList<CatalogDiscoveryEntry> Result { get; set; } = CreateEntries();

        public SearchCriteria? RequestedSearchCriteria { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task<IReadOnlyList<CatalogDiscoveryEntry>> ReadAsync(SearchCriteria searchCriteria, CancellationToken cancellationToken)
        {
            RequestedSearchCriteria = searchCriteria;

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(Result);
        }
    }

    public sealed class LookupExecutionAdmissionPortFake : ILookupExecutionAdmissionPort
    {
        public LookupExecutionAdmissionResult Result { get; set; } = LookupExecutionAdmissionResult.Acquired();

        public LookupExecutionAdmissionRequest? RequestedAdmission { get; private set; }

        public List<MessageId> CommittedCommandIds { get; } = [];

        public List<MessageId> ReleasedCommandIds { get; } = [];

        public Task<LookupExecutionAdmissionResult> TryAcquireAsync(LookupExecutionAdmissionRequest request, CancellationToken cancellationToken)
        {
            RequestedAdmission = request;
            return Task.FromResult(Result);
        }

        public Task CommitAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            CommittedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            ReleasedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    public sealed class LookupExecutionReceiptStoreFake : ILookupExecutionReceiptStore
    {
        public bool TryBeginResult { get; set; } = true;

        public List<MessageId> TryBeginCommandIds { get; } = [];

        public List<MessageId> CompletedCommandIds { get; } = [];

        public List<MessageId> ReleasedCommandIds { get; } = [];

        public Task<bool> TryBeginAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            TryBeginCommandIds.Add(messageId);
            return Task.FromResult(TryBeginResult);
        }

        public Task MarkCompletedAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            CompletedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(MessageId messageId, CancellationToken cancellationToken)
        {
            ReleasedCommandIds.Add(messageId);
            return Task.CompletedTask;
        }
    }

    public sealed class InnerHandlerFake : IHandler<LookupMusicbrainzSearchResultsMessage>
    {
        public int Calls { get; private set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task Handle(LookupMusicbrainzSearchResultsMessage request, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}
