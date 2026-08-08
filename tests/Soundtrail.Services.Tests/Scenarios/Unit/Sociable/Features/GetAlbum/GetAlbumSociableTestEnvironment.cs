using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetAlbum.Contract;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum.Support;
using Soundtrail.Services.Tests.Integration.Features.GetAlbum;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbum;

internal sealed class GetAlbumSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetAlbumHandler sut;

    private GetAlbumSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetAlbumHandler sut,
        AlbumId albumId,
        GetAlbumPortFake port)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        AlbumId = albumId;
        Port = port;
    }

    public AlbumId AlbumId { get; }

    public GetAlbumPortFake Port { get; }

    public IReadOnlyList<IMessage> SentMessages =>
        this.engine.Resolve<CommandBusFake>().SentMessages;

    public TMessage SentMessage<TMessage>() where TMessage : IMessage => this.pump.SentMessage<TMessage>();

    public IReadOnlyList<TMessage> SentMessagesOfType<TMessage>() where TMessage : IMessage =>
        this.pump.SentMessages<TMessage>();

    public static GetAlbumSociableTestEnvironment ForNoDataAvailable(AlbumId? albumId = null) =>
        Compose(albumId ?? AlbumId.From("artist-201", "album-401"), response: null);

    public static GetAlbumSociableTestEnvironment ForDataAvailable(
        AlbumId? albumId = null,
        GetAlbumResponse? response = null)
    {
        var resolvedAlbumId = albumId ?? GetAlbumScenarioData.DefaultAlbumId;
        return Compose(
            resolvedAlbumId,
            response ?? GetAlbumScenarioData.CreateResponse(albumId: resolvedAlbumId));
    }

    public Task<GetAlbumResponse?> ProjectOnChange(
        Func<GetAlbumHandler, Task<GetAlbumResponse?>> change) =>
        ProjectOnChange<GetAlbumResponse?>(change);

    public async Task<TResult> ProjectOnChange<TResult>(Func<GetAlbumHandler, Task<TResult>> change)
    {
        var result = await change(this.sut);
        await this.pump.PumpAsync();
        return result;
    }

    public GetAlbumRequest CreateRequest() => new(AlbumId);

    public void Dispose() => this.engine.Dispose();

    private static GetAlbumSociableTestEnvironment Compose(AlbumId albumId, GetAlbumResponse? response)
    {
        var engine = SociableDiscoveryEngine.Create();
        var port = engine.RequireFake<IGetAlbumPort, GetAlbumPortFake>();
        port.Seed(response);

        var sut = engine.Resolve<IApiHandler<GetAlbumRequest, GetAlbumResponse?>>() as GetAlbumHandler
            ?? throw new InvalidOperationException("GetAlbumHandler was not resolved from sociable discovery.");

        return new GetAlbumSociableTestEnvironment(engine, engine.MessagePump, sut, albumId, port);
    }
}

internal static class GetAlbumScenarioData
{
    public static AlbumId DefaultAlbumId => AlbumId.From("artist-101", "album-201");

    public static GetAlbumResponse CreateResponse(
        AlbumId? albumId = null,
        string artistName = "The Artist",
        string albumName = "The Album",
        DateOnly? releaseDate = null)
    {
        var resolvedAlbumId = albumId ?? DefaultAlbumId;
        return new GetAlbumResponse(
            ArtistId.From(resolvedAlbumId.ArtistId),
            ArtistName.From(artistName),
            resolvedAlbumId,
            albumName,
            releaseDate ?? new DateOnly(2024, 1, 2));
    }
}
