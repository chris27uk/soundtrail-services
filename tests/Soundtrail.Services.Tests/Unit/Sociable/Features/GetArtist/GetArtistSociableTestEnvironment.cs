using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Api.Features.Catalog.GetArtist;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Adapters;
using Soundtrail.Services.Api.Features.Catalog.GetArtist.Contract;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;
using Soundtrail.Services.Tests.Integration.GetArtist.Api.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetArtist;

internal sealed class GetArtistSociableTestEnvironment : IDisposable
{
    private readonly SociableDiscoveryEngine engine;
    private readonly SociableMessagePump pump;
    private readonly GetArtistHandler sut;

    private GetArtistSociableTestEnvironment(
        SociableDiscoveryEngine engine,
        SociableMessagePump pump,
        GetArtistHandler sut,
        ArtistId artistId,
        GetArtistPortFake port)
    {
        this.engine = engine;
        this.pump = pump;
        this.sut = sut;
        ArtistId = artistId;
        Port = port;
    }

    public ArtistId ArtistId { get; }

    public GetArtistPortFake Port { get; }

    public IReadOnlyList<IMessage> SentMessages =>
        engine.Resolve<CommandBusFake>().SentMessages;

    public static GetArtistSociableTestEnvironment ForNoDataAvailable(ArtistId? artistId = null) =>
        Compose(artistId ?? ArtistId.From("artist-602"), response: null);

    public static GetArtistSociableTestEnvironment ForDataAvailable(
        ArtistId? artistId = null,
        GetArtistResponse? response = null)
    {
        var resolvedArtistId = artistId ?? GetArtistScenarioData.DefaultArtistId;
        return Compose(
            resolvedArtistId,
            response ?? GetArtistScenarioData.CreateResponse(artistId: resolvedArtistId));
    }

    public async Task<TResult> ProjectOnChange<TResult>(Func<GetArtistHandler, Task<TResult>> change)
    {
        var result = await change(sut);
        await pump.PumpAsync();
        return result;
    }

    public GetArtistRequest CreateRequest() => new(ArtistId);

    public void Dispose() => engine.Dispose();

    private static GetArtistSociableTestEnvironment Compose(ArtistId artistId, GetArtistResponse? response)
    {
        var engine = SociableDiscoveryEngine.Create();
        var port = engine.RequireFake<IGetArtistPort, GetArtistPortFake>();
        port.Seed(response);

        var sut = engine.Resolve<IApiHandler<GetArtistRequest, GetArtistResponse?>>() as GetArtistHandler
            ?? throw new InvalidOperationException("GetArtistHandler was not resolved from sociable discovery.");

        return new GetArtistSociableTestEnvironment(engine, engine.MessagePump, sut, artistId, port);
    }
}

internal static class GetArtistScenarioData
{
    public static ArtistId DefaultArtistId => ArtistId.From("artist-501");

    public static GetArtistResponse CreateResponse(
        ArtistId? artistId = null,
        string artistName = "The Artist",
        string? description = "An Artist Description",
        string? imageUrl = "https://cdn.soundtrail.test/artists/artist-501.jpg")
    {
        var resolvedArtistId = artistId ?? DefaultArtistId;
        return new GetArtistResponse(
            resolvedArtistId,
            ArtistName.From(artistName),
            description,
            imageUrl);
    }
}
