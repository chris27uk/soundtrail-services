using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForAlbum.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Tests.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForAlbum;

internal sealed class GetTracksForAlbumSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetTracksForAlbumHandler sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private GetTracksForAlbumSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetTracksForAlbumHandler sut,
        AlbumId albumId,
        InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository,
        InMemoryEventStreamRepository<ArtistId> artistRepository)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        AlbumId = albumId;
        this.discoveryRepository = discoveryRepository;
        this.artistRepository = artistRepository;
    }

    public AlbumId AlbumId { get; }

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessages<TMessage>() where TMessage : IMessage => pump.SentMessages<TMessage>();

    public TEvent SavedEvent<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().First();

    public IReadOnlyList<TEvent> SavedEvents<TEvent>() where TEvent : IDomainEvent =>
        discoveryRepository.SavedEvents.Concat(artistRepository.SavedEvents).OfType<TEvent>().ToArray();

    public static GetTracksForAlbumSociableTestEnvironment ForNoExistingDataOrRequests() =>
        Compose(default, []);

    public static GetTracksForAlbumSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static GetTracksForAlbumSociableTestEnvironment ForLookupDataNotComplete() =>
        Compose(default, []);

    public static GetTracksForAlbumSociableTestEnvironment ForLookupDataNotComplete(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetTracksForAlbumSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.sut.Handle(new GetTracksForAlbumRequest(environment.AlbumId));
        await environment.pump.PumpNextMessageAsync();
        return environment;
    }

    public static GetTracksForAlbumSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteAlbumTrack[] tracks) =>
        Compose(default, tracks);

    public static GetTracksForAlbumSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteAlbumTrack[] tracks) =>
        Compose(requestTime, tracks);

    public static GetTracksForAlbumSociableTestEnvironment ForNoTracksFound() =>
        Compose(default, []);

    public static GetTracksForAlbumSociableTestEnvironment ForNoTracksFound(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetTracksForAlbumSociableTestEnvironment> ForExistingCompletedEmptyLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));
        return environment;
    }

    public static async Task<GetTracksForAlbumSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteAlbumTrack[] tracks) =>
        await ForExistingCompletedLookup(default, tracks);

    public static async Task<GetTracksForAlbumSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteAlbumTrack[] tracks)
    {
        var environment = Compose(requestTime, tracks);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetTracksForAlbumRequest(environment.AlbumId)));
        return environment;
    }

    public Task<TResult> ProjectOnChange<TResult>(Func<GetTracksForAlbumHandler, Task<TResult>> change) =>
        pump.ProjectOnChange(change, sut);

    public GetTracksForAlbumRequest CreateRequest() => new(AlbumId);

    public void Dispose() => engine.Dispose();

    private static GetTracksForAlbumSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteAlbumTrack> completedTracks)
    {
        var albumId = LookupDataCompleteAlbumTrackScenarios.DefaultAlbumId;
        var engine = SociableDiscoveryEngine.Create(utcNow);

        SeedLookupData(
            albumId,
            completedTracks,
            engine.RequireFake<IReadTracksByAlbumIdPort, ReadTracksByAlbumIdPortFake>(),
            engine.RequireFake<IReadStreamingLocationByProviderPort, ReadStreamingLocationByProviderPortFake>());

        var sut = engine.Resolve<IApiHandler<GetTracksForAlbumRequest, GetTracksForAlbumResponse?>>()
            as GetTracksForAlbumHandler
            ?? throw new InvalidOperationException("GetTracksForAlbumHandler was not resolved from sociable discovery.");

        return new GetTracksForAlbumSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            albumId,
            engine.RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(),
            engine.RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>());
    }

    private static void SeedLookupData(
        AlbumId albumId,
        IReadOnlyList<LookupDataCompleteAlbumTrack> completedTracks,
        ReadTracksByAlbumIdPortFake readTracks,
        ReadStreamingLocationByProviderPortFake readStreamingLocation)
    {
        readTracks.WithTracks(
            albumId,
            completedTracks.Select(static track => track.CatalogEntry).ToArray());

        foreach (var completedTrack in completedTracks)
        {
            if (completedTrack.CatalogEntry.Item is not CatalogItem.MusicTrack(var track))
            {
                continue;
            }

            foreach (var location in completedTrack.StreamingLocations)
            {
                readStreamingLocation.WithMetadataLocation(
                    track.ArtistName,
                    track.Title,
                    location.Key,
                    location.Value);

                if (!string.IsNullOrWhiteSpace(track.Isrc))
                {
                    readStreamingLocation.WithIsrcLocation(track.Isrc, location.Key, location.Value);
                }
            }
        }
    }
}
