using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Api.Features.Catalog.GetTrack;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetTrack.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Integration.GetTrack.Api.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTrack;

internal sealed class GetTrackSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetTrackHandler sut;

    private GetTrackSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetTrackHandler sut,
        TrackId trackId,
        GetTrackPortFake port)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        TrackId = trackId;
        Port = port;
    }

    public TrackId TrackId { get; }

    public GetTrackPortFake Port { get; }

    public IReadOnlyList<IMessage> SentMessages =>
        engine.Resolve<CommandBusFake>().SentMessages;

    public static GetTrackSociableTestEnvironment ForNoDataAvailable(TrackId? trackId = null) =>
        Compose(trackId ?? TestTrackIds.Create("track-402"), response: null);

    public static GetTrackSociableTestEnvironment ForDataAvailable(
        TrackId? trackId = null,
        GetTrackResponse? response = null)
    {
        var resolvedTrackId = trackId ?? GetTrackScenarioData.DefaultTrackId;
        return Compose(
            resolvedTrackId,
            response ?? GetTrackScenarioData.CreateResponse(trackId: resolvedTrackId));
    }

    public async Task<TResult> ProjectOnChange<TResult>(Func<GetTrackHandler, Task<TResult>> change)
    {
        var result = await change(sut);
        await pump.PumpAsync();
        return result;
    }

    public GetTrackRequest CreateRequest() => new(TrackId);

    public void Dispose() => engine.Dispose();

    private static GetTrackSociableTestEnvironment Compose(TrackId trackId, GetTrackResponse? response)
    {
        var engine = SociableDiscoveryEngine.Create();
        var port = engine.RequireFake<IGetTrackPort, GetTrackPortFake>();
        port.Seed(response);

        var sut = engine.Resolve<IApiHandler<GetTrackRequest, GetTrackResponse?>>() as GetTrackHandler
            ?? throw new InvalidOperationException("GetTrackHandler was not resolved from sociable discovery.");

        return new GetTrackSociableTestEnvironment(engine, engine.MessagePump, sut, trackId, port);
    }
}

internal static class GetTrackScenarioData
{
    public static TrackId DefaultTrackId => TestTrackIds.Create("track-201");

    public static GetTrackResponse CreateResponse(
        TrackId? trackId = null,
        string title = "The Track",
        string artistName = "The Artist",
        string? albumTitle = "The Album",
        int? durationMs = 201000,
        string? isrc = "GBAYE2400301",
        DateOnly? releaseDate = null,
        string? artworkUrl = "https://cdn.soundtrail.test/tracks/mc_track_201.jpg") =>
        new(
            trackId ?? DefaultTrackId,
            title,
            artistName,
            albumTitle,
            durationMs,
            isrc,
            releaseDate ?? new DateOnly(2024, 1, 2),
            artworkUrl,
            false,
            []);
}
