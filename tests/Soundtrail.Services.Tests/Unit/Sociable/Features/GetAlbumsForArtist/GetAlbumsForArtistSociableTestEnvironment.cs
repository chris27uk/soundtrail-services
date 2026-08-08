using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetAlbumsForArtist.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetAlbumsForArtist;

internal sealed class GetAlbumsForArtistSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetAlbumsForArtistHandler sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private GetAlbumsForArtistSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetAlbumsForArtistHandler sut,
        ArtistId artistId,
        InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository,
        InMemoryEventStreamRepository<ArtistId> artistRepository)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        ArtistId = artistId;
        this.discoveryRepository = discoveryRepository;
        this.artistRepository = artistRepository;
    }

    public ArtistId ArtistId { get; }

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage => pump.SentMessages<TMessage>();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static GetAlbumsForArtistSociableTestEnvironment ForNoExistingDataOrRequests() =>
        Compose(default, []);

    public static GetAlbumsForArtistSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static GetAlbumsForArtistSociableTestEnvironment ForLookupDataNotComplete() =>
        Compose(default, []);

    public static GetAlbumsForArtistSociableTestEnvironment ForLookupDataNotComplete(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetAlbumsForArtistSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.sut.Handle(new GetAlbumsForArtistRequest(environment.ArtistId));
        await environment.pump.PumpNextMessageAsync();
        return environment;
    }

    public static GetAlbumsForArtistSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteArtistAlbum[] albums) =>
        Compose(default, albums);

    public static GetAlbumsForArtistSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteArtistAlbum[] albums) =>
        Compose(requestTime, albums);

    public static GetAlbumsForArtistSociableTestEnvironment ForNoAlbumsFound() =>
        Compose(default, []);

    public static GetAlbumsForArtistSociableTestEnvironment ForNoAlbumsFound(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetAlbumsForArtistSociableTestEnvironment> ForExistingCompletedEmptyLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));
        return environment;
    }

    public static async Task<GetAlbumsForArtistSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteArtistAlbum[] albums) =>
        await ForExistingCompletedLookup(default, albums);

    public static async Task<GetAlbumsForArtistSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteArtistAlbum[] albums)
    {
        var environment = Compose(requestTime, albums);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetAlbumsForArtistRequest(environment.ArtistId)));
        return environment;
    }

    public Task<TResult> ProjectOnChange<TResult>(Func<GetAlbumsForArtistHandler, Task<TResult>> change) =>
        pump.ProjectOnChange(change, sut);

    public GetAlbumsForArtistRequest CreateRequest() => new(ArtistId);

    public void Dispose() => engine.Dispose();

    private static GetAlbumsForArtistSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteArtistAlbum> completedAlbums)
    {
        var artistId = LookupDataCompleteArtistAlbumScenarios.DefaultArtistId;
        var engine = SociableDiscoveryEngine.Create(utcNow);

        engine.RequireFake<IReadAlbumsByArtistIdPort, ReadAlbumsByArtistIdPortFake>()
            .WithAlbums(
                artistId,
                completedAlbums.Select(static album => album.CatalogEntry).ToArray());

        var sut = engine.Resolve<IApiHandler<GetAlbumsForArtistRequest, GetAlbumsForArtistResponse?>>()
            as GetAlbumsForArtistHandler
            ?? throw new InvalidOperationException("GetAlbumsForArtistHandler was not resolved from sociable discovery.");

        return new GetAlbumsForArtistSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            artistId,
            engine.RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(),
            engine.RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>());
    }
}
