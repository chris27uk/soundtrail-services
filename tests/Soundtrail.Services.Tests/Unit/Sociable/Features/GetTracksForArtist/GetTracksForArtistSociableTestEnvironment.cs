using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist;

internal sealed class GetTracksForArtistSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetTracksForArtistHandler sut;
    private readonly InMemoryEventStreamRepository<CatalogWorkId> discoveryRepository;
    private readonly InMemoryEventStreamRepository<ArtistId> artistRepository;

    private GetTracksForArtistSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetTracksForArtistHandler sut,
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

    public static GetTracksForArtistSociableTestEnvironment ForNoExistingDataOrRequests() =>
        Compose(default, []);

    public static GetTracksForArtistSociableTestEnvironment ForNoExistingDataOrRequests(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static GetTracksForArtistSociableTestEnvironment ForLookupDataNotComplete() =>
        Compose(default, []);

    public static GetTracksForArtistSociableTestEnvironment ForLookupDataNotComplete(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static async Task<GetTracksForArtistSociableTestEnvironment> ForExistingIncompleteLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.sut.Handle(new GetTracksForArtistRequest(environment.ArtistId));
        await environment.pump.PumpNextMessageAsync();
        return environment;
    }

    public static GetTracksForArtistSociableTestEnvironment ForLookupDataComplete(
        params LookupDataCompleteArtistTrack[] tracks) =>
        Compose(default, tracks);

    public static GetTracksForArtistSociableTestEnvironment ForLookupDataComplete(
        DateTimeOffset requestTime,
        params LookupDataCompleteArtistTrack[] tracks) =>
        Compose(requestTime, tracks);

    public static GetTracksForArtistSociableTestEnvironment ForNoTracksFound() =>
        Compose(default, []);

    public static GetTracksForArtistSociableTestEnvironment ForNoTracksFound(
        DateTimeOffset requestTime) =>
        Compose(requestTime, []);

    public static GetTracksForArtistSociableTestEnvironment ForStreamingLookupMissingTrack() =>
        Compose(
            default,
            [LookupDataCompleteArtistTrackScenarios.MidnightSignals(default)],
            StreamingLookupSeedMode.MissingTrack);

    public static GetTracksForArtistSociableTestEnvironment ForStreamingLookupTrackWithoutIsrc() =>
        Compose(
            default,
            [
                LookupDataCompleteArtistTrack.Create(
                    LookupDataCompleteArtistTrackScenarios.DefaultArtistId,
                    "Aurora Lane",
                    "No Isrc Signal",
                    "No Isrc Signal",
                    new DateOnly(2024, 1, 2),
                    null,
                    180000,
                    default,
                    isrc: null)
            ]);

    public static GetTracksForArtistSociableTestEnvironment ForStreamingLookupIncompleteMetadata() =>
        Compose(
            default,
            [LookupDataCompleteArtistTrackScenarios.MidnightSignals(default)],
            StreamingLookupSeedMode.IncompleteMetadata);

    public static GetTracksForArtistSociableTestEnvironment ForStreamingLookupWithoutProviderLink() =>
        Compose(
            default,
            [LookupDataCompleteArtistTrackScenarios.MidnightSignals(default)]);

    public static async Task<GetTracksForArtistSociableTestEnvironment> ForExistingCompletedEmptyLookup(
        DateTimeOffset requestTime = default)
    {
        var environment = Compose(requestTime, []);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetTracksForArtistRequest(environment.ArtistId)));
        return environment;
    }

    public static async Task<GetTracksForArtistSociableTestEnvironment> ForExistingCompletedLookup(
        params LookupDataCompleteArtistTrack[] tracks) =>
        await ForExistingCompletedLookup(default, tracks);

    public static async Task<GetTracksForArtistSociableTestEnvironment> ForExistingCompletedLookup(
        DateTimeOffset requestTime,
        params LookupDataCompleteArtistTrack[] tracks)
    {
        var environment = Compose(requestTime, tracks);
        await environment.ProjectOnChange(
            subject => subject.Handle(new GetTracksForArtistRequest(environment.ArtistId)));
        return environment;
    }

    public Task<TResult> ProjectOnChange<TResult>(Func<GetTracksForArtistHandler, Task<TResult>> change) =>
        pump.ProjectOnChange(change, sut);

    public GetTracksForArtistRequest CreateRequest() => new(ArtistId);

    public void Dispose() => engine.Dispose();

    private enum StreamingLookupSeedMode
    {
        Normal,
        MissingTrack,
        IncompleteMetadata
    }

    private static GetTracksForArtistSociableTestEnvironment Compose(
        DateTimeOffset utcNow,
        IReadOnlyList<LookupDataCompleteArtistTrack> completedTracks,
        StreamingLookupSeedMode streamingLookupSeedMode = StreamingLookupSeedMode.Normal)
    {
        var artistId = LookupDataCompleteArtistTrackScenarios.DefaultArtistId;
        var engine = SociableDiscoveryEngine.Create(utcNow);
        var readTrackForLookup = engine.RequireFake<IReadTrackForLookupPort, ReadTrackForLookupPortFake>();

        SeedLookupData(
            artistId,
            completedTracks,
            engine.RequireFake<IReadTracksByArtistIdPort, ReadTracksByArtistIdPortFake>(),
            engine.RequireFake<IReadStreamingLocationByProviderPort, ReadStreamingLocationByProviderPortFake>());

        if (streamingLookupSeedMode == StreamingLookupSeedMode.MissingTrack)
        {
            readTrackForLookup.SuppressWrites = true;
        }

        if (streamingLookupSeedMode == StreamingLookupSeedMode.IncompleteMetadata &&
            completedTracks.Count > 0 &&
            completedTracks[0].CatalogEntry.Item is CatalogItem.MusicTrack(var track))
        {
            readTrackForLookup.SuppressWrites = true;
            readTrackForLookup.WithLookupTrack(new TrackLookupContext(
                artistId,
                track.TrackId,
                string.Empty,
                track.ArtistName,
                track.Isrc));
        }

        var sut = engine.Resolve<IApiHandler<GetTracksForArtistRequest, GetTracksForArtistResponse?>>()
            as GetTracksForArtistHandler
            ?? throw new InvalidOperationException("GetTracksForArtistHandler was not resolved from sociable discovery.");

        return new GetTracksForArtistSociableTestEnvironment(
            engine,
            engine.MessagePump,
            sut,
            artistId,
            engine.RequireFake<IEventStreamRepository<CatalogWorkId>, InMemoryEventStreamRepository<CatalogWorkId>>(),
            engine.RequireFake<IEventStreamRepository<ArtistId>, InMemoryEventStreamRepository<ArtistId>>());
    }

    private static void SeedLookupData(
        ArtistId artistId,
        IReadOnlyList<LookupDataCompleteArtistTrack> completedTracks,
        ReadTracksByArtistIdPortFake readTracks,
        ReadStreamingLocationByProviderPortFake readStreamingLocation)
    {
        readTracks.WithTracks(
            artistId,
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
